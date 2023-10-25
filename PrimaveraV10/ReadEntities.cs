    namespace ERP
    {
        using Omnia.Libraries.Infrastructure.Behaviours;
        using System;
        using System.Collections.Generic;
        using System.Data.SqlClient;
        using System.Net.Http;
        using StdBE100;
        using ErpBS100;
        using System.Text;
        using Omnia.Libraries.Infrastructure.Behaviours.Query;
        using Microsoft.Extensions.Logging;
        using Newtonsoft.Json;
        using System.Threading.Tasks;

        public class Entities
        {
            public static async Task<List<IDictionary<string, object>>> QueryERP(string dataSource, string query, Context context)
            {
                var queryData = new ListQueryDefinition()
                {
                    ErpCompany = dataSource,
                    QueryString = query,
                    IsPaginatedQuery = false,
                    IsOmniaDbQuery = true
                };

                var queryResult = await ReadListAsync(queryData, context);
                return queryResult.Data;
            }
            public static ReadListResult ReadList(ListQueryDefinition queryDefinition, Context context) => ReadListAsync(queryDefinition, context).GetAwaiter().GetResult();
            public static async Task<ReadListResult> ReadListAsync(ListQueryDefinition queryDefinition, Context context)
            {
                var logger = context.Services.GetService<ILogger<Context>>();

                logger.LogInformation("Entered Behaviour");
                ReadListResult result = new ReadListResult();
                var data = new List<IDictionary<string, object>>();

                if (queryDefinition.Page == 0 && queryDefinition.IsPaginatedQuery)
                    throw new Exception("Page was not sent");

                if (string.IsNullOrEmpty(queryDefinition.KeyParameter) && queryDefinition.IsPaginatedQuery)
                    throw new Exception("Key Parameter was not sent");

                var filterSql = new StringBuilder();
                var sortSql = new StringBuilder();

                var parameters = new List<SqlParameter>();

                if (queryDefinition.QueryContext != null)
                {
                    queryDefinition.QueryString = queryDefinition.QueryContext.QueryDefinition?.Expression;
                    BuildFilter(queryDefinition.QueryContext.Filter);
                    BuildSort();

                    foreach (var parameter in queryDefinition.QueryContext.Parameters)
                        queryDefinition.Parameters.Add(parameter.Name, parameter.Value?.ToString());
                }

                if (string.IsNullOrEmpty(queryDefinition.QueryString))
                    throw new ArgumentNullException("Missing query to execute ReadList");

                if (queryDefinition.IsOmniaDbQuery && !queryDefinition.Parameters.ContainsKey("PRIMAVERA_COMPANY"))
                    queryDefinition.Parameters.Add("PRIMAVERA_COMPANY", queryDefinition.ErpCompany);

                // Apply parameters
                foreach (var entry in queryDefinition.Parameters)
                {
                    var parameterName = $"@parameter_{entry.Key}";
                    queryDefinition.QueryString = queryDefinition.QueryString.Replace($"'@{entry.Key}@'", parameterName).Replace($"@{entry.Key}@", parameterName);

                    if (entry.Value is null)
                        parameters.Add(new SqlParameter(parameterName, DBNull.Value));
                    else
                        parameters.Add(new SqlParameter(parameterName, entry.Value));
                }

                var filterSqlAsString = filterSql.ToString();

                string query = queryDefinition.QueryString;

                if (queryDefinition.IsPaginatedQuery)
                {
                    query = "SELECT * FROM " +
        "(SELECT Count(@OMNIA_QUERYKEYPARAMETER@) over() as _OMNIA_NUMBER_OF_ROWS_, ROW_NUMBER() OVER(ORDER BY @OMNIA_QUERYSORT@) AS _OMNIA_ROW_NUMBER_, INNER_TBL.* FROM ( " +
        "@OMNIA_QUERY@ " +
        ") AS INNER_TBL " +
        (string.IsNullOrEmpty(filterSqlAsString) ? "" : "WHERE (" + filterSql.ToString() + ") ") +
        ") AS TBL " +
        "WHERE _OMNIA_ROW_NUMBER_ BETWEEN ((@OMNIA_PAGE@ - 1) * @OMNIA_ROWS@ + 1) AND (@OMNIA_PAGE@ * @OMNIA_ROWS@) ";


                    query = query.Replace("@OMNIA_QUERY@", queryDefinition.QueryString)
                        .Replace("@OMNIA_PAGE@", queryDefinition.Page.ToString())
                        .Replace("@OMNIA_ROWS@", queryDefinition.Rows.ToString())
                        .Replace("@OMNIA_QUERYKEYPARAMETER@", queryDefinition.KeyParameter.ToString())
                        .Replace("@OMNIA_QUERYSORT@", OrderByClause());
                }

                await QueryDatabase(queryDefinition.IsOmniaDbQuery);

                logger.LogInformation("Going to exit Behaviour");

                return result;

                async Task QueryDatabase(bool isOmniaDbQuery)
                {
                    string connectionString = await GetConnectionString(context, isOmniaDbQuery, queryDefinition.ErpCompany);

                    int numberOfRecords = 0;

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            if (parameters.Count > 0)
                                command.Parameters.AddRange(parameters.ToArray());

                            using (SqlDataReader dataReader = await command.ExecuteReaderAsync())
                            {
                                if (dataReader.HasRows)
                                {
                                    while (await dataReader.ReadAsync())
                                    {
                                        if (!queryDefinition.IsPaginatedQuery)
                                            numberOfRecords++;
                                        else if (numberOfRecords == 0)
                                            numberOfRecords = Convert.ToInt32(dataReader["_OMNIA_NUMBER_OF_ROWS_"]);

                                        Dictionary<string, object> entityData = new Dictionary<string, object>();
                                        for (short j = 0; j < dataReader.FieldCount; j++)
                                        {
                                            var nome = dataReader.GetName(j);
                                            if (!nome.Equals("_OMNIA_NUMBER_OF_ROWS_") && !nome.Equals("_OMNIA_ROW_NUMBER_"))
                                                entityData.Add(nome, dataReader[nome]);
                                        }
                                        data.Add(entityData);
                                    }
                                }
                            }
                        }
                    }

                    result.NumberOfRecords = numberOfRecords;
                    result.Data = data;
                }

                string OrderByClause()
                {
                    var sortSqlText = sortSql.ToString();

                    if (!string.IsNullOrEmpty(sortSqlText))
                        return sortSqlText;

                    if (string.IsNullOrEmpty(queryDefinition.QuerySort))
                        return queryDefinition.KeyParameter.ToString();

                    return queryDefinition.QuerySort;
                }

                void BuildFilter(QueryRequestFilter queryFilter)
                {
                    if (queryFilter == null) return;

                    switch (queryFilter)
                    {
                        case UnaryQueryRequestFilter unaryFilter:
                            {
                                filterSql.Append(unaryFilter.Path);

                                if (unaryFilter.Value == null)
                                {
                                    filterSql.Append(" IS NULL ");
                                    break;
                                }

                                switch (unaryFilter.Operator)
                                {
                                    case QueryComparisonOperator.EqualTo:
                                        filterSql.Append(" = ");
                                        break;
                                    case QueryComparisonOperator.GreaterThan:
                                        filterSql.Append(" > ");
                                        break;
                                    case QueryComparisonOperator.LessThan:
                                        filterSql.Append(" < ");
                                        break;
                                    case QueryComparisonOperator.GreaterThanOrEqualTo:
                                        filterSql.Append(" >= ");
                                        break;
                                    case QueryComparisonOperator.LessThanOrEqualTo:
                                        filterSql.Append(" <= ");
                                        break;
                                    case QueryComparisonOperator.NotEqualTo:
                                        filterSql.Append(" <> ");
                                        break;
                                    case QueryComparisonOperator.Like:
                                    case QueryComparisonOperator.EndsWith:
                                        filterSql.Append(" LIKE CONCAT('%', ");
                                        break;
                                    case QueryComparisonOperator.NotEndsWith:                                    
                                    case QueryComparisonOperator.NotLike:
                                        filterSql.Append(" NOT LIKE CONCAT('%', ");
                                        break;
                                    case QueryComparisonOperator.StartsWith:
                                        filterSql.Append(" LIKE CONCAT( ");
                                        break;
                                    case QueryComparisonOperator.NotStartsWith:
                                        filterSql.Append(" NOT LIKE CONCAT( ");
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                var parameterName = $"@filter_{unaryFilter.Path}_{Guid.NewGuid().ToString("N")}";
                                filterSql.Append(parameterName);

                                if (unaryFilter.Operator.Equals(QueryComparisonOperator.Like) || unaryFilter.Operator.Equals(QueryComparisonOperator.NotLike) || unaryFilter.Operator.Equals(QueryComparisonOperator.StartsWith) || unaryFilter.Operator.Equals(QueryComparisonOperator.NotStartsWith))
                                    filterSql.Append(", '%')");

                                if (unaryFilter.Operator.Equals(QueryComparisonOperator.EndsWith) || unaryFilter.Operator.Equals(QueryComparisonOperator.NotEndsWith))
                                    filterSql.Append(")");

                                parameters.Add(new SqlParameter(parameterName, unaryFilter.Value));
                            }
                            break;

                        case BinaryQueryRequestFilter binaryFilter:
                            {
                                filterSql.Append('(');
                                BuildFilter(binaryFilter.Left);

                                switch (binaryFilter.Operator)
                                {
                                    case QueryLogicalOperator.And:
                                        filterSql.Append(" AND ");
                                        break;
                                    case QueryLogicalOperator.Or:
                                        filterSql.Append(" OR ");
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                BuildFilter(binaryFilter.Right);
                                filterSql.Append(')');
                            }
                            break;
                    }
                }

                void BuildSort()
                {
                    if (queryDefinition.QueryContext.Sort == null || queryDefinition.QueryContext.Sort.Length == 0) return;

                    for (var i = 0; i < queryDefinition.QueryContext.Sort.Length; i++)
                    {
                        if (i > 0)
                            sortSql.Append(", ");

                        sortSql.Append(queryDefinition.QueryContext.Sort[i].Property)
                            .Append(" ");

                        switch (queryDefinition.QueryContext.Sort[i].Direction)
                        {
                            case QuerySort.Ascend:
                                sortSql.Append("ASC");
                                break;
                            case QuerySort.Descend:
                                sortSql.Append("DESC");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            public static IDictionary<string, object> Read(ListQueryDefinition queryDefinition, Context context) => ReadAsync(queryDefinition, context).GetAwaiter().GetResult();
            public static async Task<IDictionary<string, object>> ReadAsync(ListQueryDefinition queryDefinition, Context context)
            {
                return await QueryDatabase(queryDefinition.IsOmniaDbQuery);

                async Task<Dictionary<string, object>> QueryDatabase(bool isOmniaDbQuery)
                {
                    string connectionString = await GetConnectionString(context, isOmniaDbQuery, queryDefinition.ErpCompany);

                    Dictionary<string, object> entityData = new Dictionary<string, object>();

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (SqlCommand command = new SqlCommand(queryDefinition.QueryString, connection))
                        using (SqlDataReader dataReader = await command.ExecuteReaderAsync())
                        {
                            if (dataReader.HasRows)
                            {
                                await dataReader.ReadAsync();
                                for (short j = 0; j < dataReader.FieldCount; j++)
                                {
                                    var nome = dataReader.GetName(j);
                                    entityData.Add(nome, dataReader[nome]);
                                }
                            }
                            else
                            {
                                throw new Exception(await Cache.Languages.GetTranslationAndFormatAsync(context, "NoResultsFound"));
                            }
                        }
                    }

                    return entityData;
                }
            }

            private static async Task<string> GetConnectionString(Context context, bool isOmniaDbQuery, string erpCompany)
            {
                string databaseName;
                var settingsDto = await Cache.SettingsCache.GetSettingsAsync(context);

                if (isOmniaDbQuery)
                    databaseName = $"OMNIA_{settingsDto.DatabaseSuffix}";
                else
                    databaseName = $"PRI{(string.IsNullOrEmpty(erpCompany) ? context.Operation.DataSource : erpCompany)}";

                return await Connection.GetConnectionStringAsync(context, databaseName, settingsDto.DatabaseInstance);
            }
        }

        public class ReadListResult
        {
            public int NumberOfRecords { get; set; }
            public List<IDictionary<string, object>> Data { get; set; }
        }

        public class ListQueryDefinition
        {
            public string ErpCompany { get; set; }
            public QueryContext QueryContext { get; set; }
            public string QueryString { get; set; }
            public string QuerySort { get; set; }
            public int? Page { get; set; } = 0;
            public int? Rows { get; set; } = 25;
            public string KeyParameter { get; set; }
            public bool IsOmniaDbQuery { get; set; } = false;
            public bool IsPaginatedQuery { get; set; } = true;
            public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        }
    }
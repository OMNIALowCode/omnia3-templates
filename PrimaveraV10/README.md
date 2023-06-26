# PRIMAVERA ERP V10 Micro Template

Template to add facilitators to start working with the PRIMAVERA ERP V10.

## Getting started

1. Import to your OMNIA tenant the template Zip;

> https://github.com/OMNIALowCode/omnia3-templates/blob/master/PrimaveraV10/Template/PrimaveraV10Template.zip?raw=true

2. Build & Deploy the model;
3. Setup an OMNIA Connector in the PRIMAVERA Server;
4. Go to the Application and create a new instance of the Primavera entity with your connection data;

## Create a List over the ERP Database

Create a new entity with the _Primavera_ data source and add the following code block to the Read List Data behaviour.

```C#

var queryData = new ReadEntities.ListQueryDefinition()
{
    ErpCompany = this._Context.Operation.DataSource,
    QueryContext = queryContext,
    Page = page,
    Rows = pageSize,
    KeyParameter = "_code"
};
var queryResult = await ReadEntities.Entities.ReadListAsync(queryData, this._Context);
return (queryResult.NumberOfRecords, queryResult.Data);

```

Then, you can change the Query of that entity, defining an Advanced SQL Query that will be executed in the PRIMAVERA Database.

```SQL
SELECT
    Armazem as _code,
    Descricao as _name,
    Morada as Address,
    Localidade as City
FROM Armazens
```

## Production scenarios

In production, scenarios are recommended to generate a new Encryption Key and change it in the _Primavera_GetEncryptionKey_ Application Behaviour.

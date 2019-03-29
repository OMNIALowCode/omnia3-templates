# PRIMAVERA ERP V9 Micro Template

Template to add facilitators to start working with the PRIMAVERA ERP V9.


## Getting started

 1. Import to your OMNIA tenant the template Zip;

> https://github.com/OMNIALowCode/omnia3-templates/blob/master/PrimaveraV9/Template/PrimaveraV9Template.zip?raw=true

 2. Build & Deploy the model;
 3. Setup an OMNIA Connector in the PRIMAVERA Server;
 3. Go to the Application and create a new instance of the Primavera entity with your connection data;


## Open PRIMAVERA Company

In the behaviour where you want to access to the ERP, use the following code block.


```C#

var company = "";

IDictionary<string, object> erpBsResponse = PrimaveraApplicationBehaviours.Primavera_OpenCompany(
    new Dictionary<string, object>()
    {
        {"Company", company}
    }, 
    this._Context);

if (!erpBsResponse.ContainsKey("ErpBS"))
        throw new Exception("Error opening ERP Company");
```

## Create a List over the ERP Database

Create a new entity with the _Primavera_ data source and add the following code block to the Read List Data behaviour.

```C#

Dictionary<string, object> queryData = new Dictionary<string, object> {
    { "Company", this._Context.Operation.DataSource },
    { "QueryContext", queryContext } ,
    { "Page", page }, { "Rows", pageSize },
    { "KeyParameter", "_code" }};

var queryResult = PrimaveraApplicationBehaviours.Primavera_ReadList(queryData, this._Context);

return ((int)queryResult["NumberOfRecords"], (List<IDictionary<string, object>>)queryResult["Data"]);

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

In production, scenarios are recommended to generate a new Encryption Key and change it in the _Primavera\_GetEncryptionKey_ Application Behaviour.
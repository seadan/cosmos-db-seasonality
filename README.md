# cosmos-db-seasonality
This is a project to configure the Cosmos DB capacity per hour using an Azure Function, the user can configure the desired RU their Cosmos DB would need to have each our based on the seasonality usage of the database. 

This project is based on LuisBozquez Cosmos DB Offer project: https://github.com/LuisBosquez/azure-cosmos-db-dotnet-functions-ru-setting. 

A sample configuration of how the seasonality is defined can be found on the local.settings.json configuration file.

The property called "DbRuDefinition", consist on a three part definition: 
    - Cosmos DB Database
    - Cosmos DB Collection
    - The RU setting for each hour on a 24hr schedule
Example:
    "DbRuDefinition": "taskDatabase;TaskCollection;400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 22000, 2300, 2400, 2500, 2600, 2700;"


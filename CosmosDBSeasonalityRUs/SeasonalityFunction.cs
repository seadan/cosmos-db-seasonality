using System;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CosmosDBSeasonalityRUs
{
    public static class SeasonalityFunction
    {
        // Default values. Use your own.
        static int MAX_THROUGHPUT_CAPACITY = 10000;
        static int MIN_THROUGHPUT_CAPACITY = 400;

        [FunctionName("SeasonalityFunction")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            string ConnectionString = GetEnvironmentVariable("ConnectionString");

            if (ConnectionString == "ConnectionString:")
            {
                log.LogError("No Connection String for CosmosDB was configured");
                return;
            }
            else
            {
                ConnectionString = ConnectionString.Substring(17);
            }

            string dbRUDefinition = GetEnvironmentVariable("DbRuDefinition");
            if (ConnectionString == "DbRuDefinition:")
            {
                log.LogError("No DB RU Definition for CosmosDB was configured");
                return;
            }
            else
            {
                dbRUDefinition = dbRUDefinition.Substring(15);
            }

            string[] config = dbRUDefinition.Split(";");

            DbSeasonality[] dbs = { new DbSeasonality(config) };


            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation($"Current hour: {DateTime.Now.Hour}");
            CosmosClient cosmosClient = new CosmosClient(ConnectionString);


            log.LogInformation($"Setting the capacity for database: {dbs[0].DatabaseName} and container: {dbs[0].ContainerName} to {dbs[0].RuCapacity(DateTime.Now.Hour)}.");

            if (UpdateContainerOffer(cosmosClient, dbs[0].DatabaseName, dbs[0].ContainerName, dbs[0].RuCapacity(DateTime.Now.Hour)))
            {
                log.LogInformation($"Successfully set throughput to {dbs[0].RuCapacity(DateTime.Now.Hour)} RU's for database: {dbs[0].DatabaseName} and container: {dbs[0].ContainerName}.");
            }

            // Peak hour RU maximization    
            /**
             * This is another approach when you want to configure only two levels of RU utilization MAX and MIN
             * you can define the hour in which grows to Max
             * and the time in which reduces to Min.
             * its simpler and the code is below:
             * */
            /*
           if (DateTime.Now.Hour.ToString() == "19")
           {
               log.LogInformation($"Maximizing capacity for database: {0} and container: {1}.", DatabaseName, ContainerName);
               if (MaximizeContainerOffer(cosmosClient, DatabaseName, ContainerName))
               {
                   log.LogInformation($"Successfully set throughput to {3} RU's for database: {0} and container: {1}.", DatabaseName, ContainerName, MAX_THROUGHPUT_CAPACITY);
               }
           }
           */
            // Slow hour minimization
            /*if (DateTime.Now.Hour.ToString() == "20")
            {
                log.LogInformation($"Minimizing capacity for database: {0} and container: {1}.", DatabaseName, ContainerName);
                if(MinimizeContainerOffer(cosmosClient, DatabaseName, ContainerName))
                {
                    log.LogInformation($"Successfully set throughput to {3} RU's for database: {0} and container: {1}.", DatabaseName, ContainerName, MIN_THROUGHPUT_CAPACITY);
                }
            }*/

            log.LogInformation("Current throughput {0}", GetContainerOffer(cosmosClient, dbs[0].DatabaseName, dbs[0].ContainerName));
        }


        public static int GetContainerOffer(CosmosClient client, string databaseName, string containerName)
        {
            Container container = client.GetContainer(databaseName, containerName);

            return (int)container.ReadThroughputAsync().Result;
        }

        public static bool UpdateContainerOffer(CosmosClient client, string databaseName, string containerName, int updatedContainerOffer)
        {
            Container container = client.GetContainer(databaseName, containerName);
            HttpStatusCode resultCode;

            try
            {
                resultCode = container.ReplaceThroughputAsync(updatedContainerOffer).Result.StatusCode;
            }
            catch (CosmosException ce)
            {
                Console.WriteLine(ce.Message);
                return false;
            }

            if (resultCode.Equals(HttpStatusCode.OK))
            {
                Console.WriteLine((int)container.ReadThroughputAsync().Result);
                return true;
            }

            return false;
        }

        public static bool IncreaseContainerOffer(CosmosClient client, string databaseName, string containerName, int increase)
        {
            Container container = client.GetContainer(databaseName, containerName);

            int currentOffer = (int)container.ReadThroughputAsync().Result;

            return UpdateContainerOffer(client, databaseName, containerName, currentOffer + increase);
        }

        public static bool MaximizeContainerOffer(CosmosClient client, string databaseName, string containerName)
        {
            Container container = client.GetContainer(databaseName, containerName);

            int currentOffer = (int)container.ReadThroughputAsync().Result;

            return UpdateContainerOffer(client, databaseName, containerName, MAX_THROUGHPUT_CAPACITY);
        }

        public static bool MinimizeContainerOffer(CosmosClient client, string databaseName, string containerName)
        {
            Container container = client.GetContainer(databaseName, containerName);

            int currentOffer = (int)container.ReadThroughputAsync().Result;

            return UpdateContainerOffer(client, databaseName, containerName, MIN_THROUGHPUT_CAPACITY);
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }

    class DbSeasonality
    {

        private static int DEFAULT_THROUGHPUT_CAPACITY = 1000;
        private string[] throughputCapacityArray; //= { 400, 500, 600, 700, 800, 900, 1000, 900, 800, 700, 600, 500, 400, 500, 600, 700, 800, 900, 1000, 900, 800, 700, 600, 500 };

        public DbSeasonality(string[] config)
        {
            DatabaseName = config[0].Trim();
            ContainerName = config[1].Trim();
            throughputCapacityArray = config[2].Split(",");
        }

        public DbSeasonality()
        {
        }

        public string DatabaseName { get; set; }// = "taskDatabase";
        public string ContainerName { get; set; }// = "TaskCollection";



        public int RuCapacity(int index)
        {

            if (throughputCapacityArray != null || index <= throughputCapacityArray.Length)
            {
                try
                {
                    int result = Int32.Parse(throughputCapacityArray[index - 1]);
                    return result;
                }
                catch (FormatException)
                {
                    Console.WriteLine($"Unable to parse '{throughputCapacityArray[index - 1]}'");
                    return DEFAULT_THROUGHPUT_CAPACITY;
                }
            }
            else
            {
                return DEFAULT_THROUGHPUT_CAPACITY;
            }

        }
    }

}

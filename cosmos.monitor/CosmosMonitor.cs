using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace cosmos.monitor
{
    public class CosmosMonitor
    {
        private ChangeFeedEstimator estimator; 

        /// <summary>
        /// Creates an Estimator to estimate the Processor progress
        /// </summary>
        private ChangeFeedEstimator GetOrCreateEstimator(CosmosClient cosmosClient)
        {
            this.estimator ??= cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DATABASE"), Environment.GetEnvironmentVariable("EVENTCONTAINER"))
                .GetChangeFeedEstimator("EventProcessor", cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DATABASE"), "leases"));

            return this.estimator;
        }

        [FunctionName("CosmosMonitor")]
        public async Task Run([TimerTrigger("*/30 * * * * *")]TimerInfo timerInfo,
            [CosmosDB(
            databaseName: "%DATABASE%",
            containerName: "%EVENTCONTAINER%",
            PreferredLocations = "%REGION%",
            Connection = "cosmosConnection")]CosmosClient cosmosClient,
            ILogger log)
        {
            var estimator = GetOrCreateEstimator(cosmosClient).GetCurrentStateIterator();
            while (estimator.HasMoreResults)
            {
                var response = await estimator.ReadNextAsync();
                foreach (var item in response)
                {
                    log.LogMetric($"Estimatio Lease {item.LeaseToken}", item.EstimatedLag);
                }
            }
        }
    }
}

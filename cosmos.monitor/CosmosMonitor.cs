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
            var leaseContainer = cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DATABASE"), "leases");
            var monitoredContainer = cosmosClient.GetContainer(Environment.GetEnvironmentVariable("DATABASE"), Environment.GetEnvironmentVariable("EVENTCONTAINER"));
            this.estimator ??= monitoredContainer.GetChangeFeedEstimator("EventProcessor", leaseContainer);

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
                    log.LogMetric($"Estimation Lease {item.LeaseToken}", item.EstimatedLag, 
                        new Dictionary<string, object>() { 
                            { "Lease", item.LeaseToken }, 
                            { "Owner", item.InstanceName } });
                }
            }
        }
    }
}

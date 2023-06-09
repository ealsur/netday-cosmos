using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace cosmos.trigger
{
    public static class CosmosProcessor
    {
        // Support for retries, see: https://learn.microsoft.com/azure/azure-functions/functions-bindings-error-pages#retry-strategies
        [FixedDelayRetry(3, "00:00:05")]
        [FunctionName("CosmosProcessor")]
        public static async Task Run(
            // Trigger to react to events
            [CosmosDBTrigger(
            databaseName: "%DATABASE%",
            containerName: "%EVENTCONTAINER%",
            Connection = "cosmosConnection",
            LeaseContainerName = "leases",
            PreferredLocations = "%REGION%",
            MaxItemsPerInvocation = 1000,
            LeaseContainerPrefix = "EventProcessor")]IReadOnlyList<DeviceTelemetry> events,

            // Output binding to generate summary
            [CosmosDB(
            databaseName: "%DATABASE%",
            containerName: "%SUMMARYCONTAINER%",
            PreferredLocations = "%REGION%",
            Connection = "cosmosConnection")]IAsyncCollector<Summary> summary,

            ILogger log)
        {
            foreach (var group in events.GroupBy(singleEvent => singleEvent.DeviceId))
            {
                log.LogInformation($"Generating summary for device {group.Key}...");
                await summary.AddAsync(new Summary()
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = group.Key,
                    Time = DateTime.UtcNow.ToString("s"),
                    MaxValue = group.Max(item => item.Value)
                });
            }
        }


        public class DeviceTelemetry
        {
            public string DeviceId { get; set; }

            public double Value { get; set; }
        }

        public class Summary
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            public string DeviceId { get; set; }

            public string Time { get; set; }

            public double MaxValue { get; set; }
        }
    }
}

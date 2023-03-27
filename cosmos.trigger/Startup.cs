using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.CosmosDB;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

[assembly: FunctionsStartup(typeof(cosmos.trigger.Startup))]
namespace cosmos.trigger
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Customize Cosmos DB serializer
            builder.Services.AddSingleton<ICosmosDBSerializerFactory, MyCosmosDBSerializerFactory>();
        }
    }

    public class MyCosmosDBSerializerFactory : ICosmosDBSerializerFactory
    {
        public CosmosSerializer CreateSerializer()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                // Other System.Text.Json custom confits
            };

            return new CustomSystemTextJsonSerializer(options);
        }
    }
}

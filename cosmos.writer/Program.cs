using CommandLine;
using Microsoft.Azure.Cosmos;

await Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
    .WithParsedAsync<Options>(async o =>
    {

        using CosmosClient client = new(o.ConnectionString);
        using SemaphoreSlim concurrencySemaphore = new(o.ConcurrentWrites);
        using CancellationTokenSource cts = new(o.RunTimeAsTimespan);

        Random random = new Random((int)DateTime.UtcNow.Ticks);

        List<string> devices = new List<string>(o.Devices);

        for (int i = 0; i < o.Devices; i++)
        {
            devices.Add($"Device{i}");
        }

        Container container = client.GetContainer(o.Database, o.Container);

        List<Task> operations = new List<Task>();
        while (!cts.IsCancellationRequested)
        {
            try
            {
                await concurrencySemaphore.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            string deviceId = devices[random.Next(0, devices.Count - 1)];

            DeviceTelemetry telemetry = new()
            {
                id = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                Value = random.NextDouble(),
            };

            operations.Add(container.CreateItemAsync(telemetry, new PartitionKey(telemetry.id), cancellationToken: cts.Token).ContinueWith(task =>
            {
                concurrencySemaphore.Release();

                if (!task.IsCompletedSuccessfully
                    && task.Exception != null)
                {
                    Console.WriteLine(task.Exception.ToString());
                }
            }));

        }

        await Task.WhenAll(operations);
    });



class DeviceTelemetry
{
    public string id { get; set; }

    public string DeviceId { get; set; }

    public double Value { get; set; }
}


class Options
{
    [Option('a', "connectionString", Required = true, HelpText = "Connection string.")]
    public string? ConnectionString { get; set; }

    [Option('c', "concurrency", Required = true, HelpText = "Degree of concurrency")]
    public int ConcurrentWrites { get; set; }

    [Option('d', "devices", Required = true, HelpText = "Number of devices to simulate")]
    public int Devices { get; set; }

    [Option('b', "db", Required = true, HelpText = "Database")]
    public string? Database { get; set; }

    [Option('o', "container", Required = true, HelpText = "Container")]
    public string? Container { get; set; }

    [Option('t', "time", Required = false, HelpText = "Run time")]
    public string RunTime
    {
        get => this.RunTimeAsTimespan.ToString();
        set => this.RunTimeAsTimespan = System.Xml.XmlConvert.ToTimeSpan(value);
    }

    internal TimeSpan RunTimeAsTimespan { get; private set; } = TimeSpan.FromSeconds(60);
}

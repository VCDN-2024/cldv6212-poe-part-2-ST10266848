using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        //services.AddLogging();

        //Register TableClient in Function
        services.AddSingleton<TableClient>(provider =>
        {
            // Initialize your TableClient here, e.g. using connection strings
            return new TableClient("AzureWebJobsStorage", "Products");
        });

        // Register BlobServiceClient for Function
        services.AddSingleton(x => new BlobServiceClient("AzureWebJobsStorage"));
        //services.AddSingleton(new QueueClient("AzureWebJobsStorage", "notifications"));
        services.AddSingleton<QueueClient>(provider =>
        {
            return new QueueClient("AzureWebJobsStorage", "notifications");
        });

    })
    .Build();

host.Run();

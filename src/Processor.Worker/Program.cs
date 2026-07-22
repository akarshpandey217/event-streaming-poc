using Processor.Worker;
using Npgsql;
using Shared;

var builder = Host.CreateApplicationBuilder(args);

var pubSubOptions = builder.Configuration.GetSection("PubSub").Get<PubSubOptions>() ?? new PubSubOptions();
var processorOptions = builder.Configuration.GetSection("Processor").Get<ProcessorOptions>() ?? new ProcessorOptions();
var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured");

builder.Services.AddSingleton(pubSubOptions);
builder.Services.AddSingleton(processorOptions);
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

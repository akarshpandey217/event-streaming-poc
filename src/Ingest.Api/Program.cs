using Ingest.Api;
using Ingest.Api.DTO;
using Shared;
using Prometheus;
using Google.Api;

var builder = WebApplication.CreateBuilder(args);

var pubSubOptions = builder.Configuration.GetSection("PubSub").Get<PubSubOptions>() ?? new PubSubOptions();

builder.Services.AddSingleton(pubSubOptions);
builder.Services.AddSingleton(await PubSubClientFactory.CreatePublisherAsync());
builder.Services.AddSingleton<IEventPublisher>(sp =>
    new PubSubEventPublisher(
        sp.GetRequiredService<Google.Cloud.PubSub.V1.PublisherServiceApiClient>(), pubSubOptions)
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

await PubSubClientFactory.EnsureTopicAndSubscriptionAsync(pubSubOptions);

app.UseHttpMetrics();

var eventsAccepted = Metrics.CreateCounter(
    "ingest_events_accepted_total", "Events accepted and published to pubsub",
    new CounterConfiguration{LabelNames = new []{"tenant_id", "event_type"}}
);

var eventsRejected = Metrics.CreateCounter(
    "ingest_events_rejected_total", "Events rejected by validation"
);

app.MapGet("/health", ()=>Results.Ok(new {status = "ok"})).WithName("Health").WithOpenApi();

app.MapMetrics();

app.MapPost("/events", async (EventRequest request, HttpRequest httpReq, IEventPublisher publisher) =>
{
    var tenant_id = httpReq.Headers["X-Tenant-Id"].ToString();
    var error = EventRequestValidator.Validate(request, tenant_id, out var evnt);
    if(error is not null || evnt is null)
    {
        eventsRejected.Inc();
        return Results.BadRequest(new {error});
    }
    await publisher.PublishAsync(evnt, httpReq.HttpContext.RequestAborted);
    eventsAccepted.WithLabels(tenant_id, evnt.EventType.ToString()).Inc();
    return Results.Accepted(value: new {eventId = evnt.EventId});
}).WithDisplayName("PostEvents")
.WithOpenApi();

app.MapPost("/events/batch", async (List<EventRequest> requests, HttpRequest httpReq, IEventPublisher publisher) =>
{
    var tenant_id = httpReq.Headers["X-Tenant-Id"].ToString();
    var accepted = new List<Guid>();
    var errors = new List<string>();

    foreach (var request in requests)
    {
        var error = EventRequestValidator.Validate(request, tenant_id, out var evnt);
        if(error is not null || evnt is null)
        {
            eventsRejected.Inc();
            return Results.BadRequest(new {error});
        }
        await publisher.PublishAsync(evnt, httpReq.HttpContext.RequestAborted);
        eventsAccepted.WithLabels(tenant_id, evnt.EventType.ToString()).Inc();
        accepted.Add(evnt.EventId);
    }
    
    return Results.Accepted(value: new {accepted, errors});
}).WithDisplayName("PostEvents")
.WithOpenApi();

app.Run();
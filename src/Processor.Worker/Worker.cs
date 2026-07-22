using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Shared;

namespace Processor.Worker;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly PubSubOptions _pubSubOptions;
    private readonly EventProcessor _eventProcessor;

    public Worker(PubSubOptions pubSubOptions, EventProcessor eventProcessor, ILogger<Worker> logger)
    {
        _logger = logger;
        _pubSubOptions = pubSubOptions;
        _eventProcessor = eventProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PubSubClientFactory.EnsureTopicAndSubscriptionAsync(_pubSubOptions);
        var subscriptionName = SubscriptionName.FromProjectSubscription(_pubSubOptions.ProjectId,
        _pubSubOptions.SubscriptionId);
        var subscriber = await new SubscriberClientBuilder
        {
            SubscriptionName = subscriptionName,
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync(stoppingToken);
        _logger.LogInformation("Listening on subscription {Subscription}", subscriptionName);

        using var registration = stoppingToken.Register(() => _ = StopAsync(stoppingToken));
        await subscriber.StartAsync(async (message, ct) =>
        {
            RetailEvent? evnt;
            try
            {
                evnt = JsonSerializer.Deserialize<RetailEvent>(message.Data.ToStringUtf8());
            }
            catch(JsonException ex)
            {
                _logger.LogError(ex, "Dropping message as it can not be parsed {MessageId}", message.MessageId);
                return SubscriberClient.Reply.Ack;
            }
            if(evnt is null)
            {
                _logger.LogWarning("Dropping empty message {MessageId}", message.MessageId);
                return SubscriberClient.Reply.Ack;
            }
            try
            {
                await _eventProcessor.ProcessAsync(evnt, ct);
                return SubscriberClient.Reply.Ack;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {eventId} {EventType} - will retry", evnt.EventId, evnt.EventType);
                return SubscriberClient.Reply.Nack;
            }
        });
    }

}

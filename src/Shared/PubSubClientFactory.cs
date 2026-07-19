using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Grpc.Core;

namespace Shared;

public class PubSubOptions
{
    public string ProjectId { get; set; } = "retail-streaming-lite";
    public string TopicId { get; set; } = "retail-events";
    public string SubscriptionId { get; set; } = "retail-events-worker";
}

public static class PubSubClientFactory
{
    public static async Task<PublisherServiceApiClient> CreatePublisherAsync()
    {
        return await new PublisherServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync();
    }

    public static async Task<SubscriberServiceApiClient> CreateSubscriberAdminAsync()
    {
        return await new SubscriberServiceApiClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction
        }.BuildAsync();
    }

    public static async Task EnsureTopicAndSubscriptionAsync(PubSubOptions options)
    {
        var topicName = TopicName.FromProjectTopic(options.ProjectId, options.TopicId);
        var subscriptionName = SubscriptionName.FromProjectSubscription(options.ProjectId, options.SubscriptionId);

        var publisherAdmin = await CreatePublisherAsync();
        try
        {
            await publisherAdmin.CreateTopicAsync(topicName);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            //already exists
        }

        var subscriberAdmin = await CreateSubscriberAdminAsync();
        try
        {
            var subscription = new Subscription
            {
                SubscriptionName = subscriptionName,
                TopicAsTopicName = topicName,
                AckDeadlineSeconds = 20,
                EnableMessageOrdering = true
            };
            await subscriberAdmin.CreateSubscriptionAsync(subscription);
        }
        catch(RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            //already exists
        }
    }
}
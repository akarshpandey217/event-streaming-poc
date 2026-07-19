using System.Text.Json;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;

namespace Shared;

public interface IEventPublisher
{
    Task PublishAsync(RetailEvent evnt, CancellationToken ct = default);
}

public class PubSubEventPublisher: IEventPublisher
{
    private readonly PublisherServiceApiClient _client;
    private readonly TopicName _topicName;
    public PubSubEventPublisher(PublisherServiceApiClient client, PubSubOptions options)
    {
        _client = client;
        _topicName = TopicName.FromProjectTopic(options.ProjectId, options.TopicId);
    }
    public async Task PublishAsync(RetailEvent evnt, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(evnt);
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(json),
            OrderingKey = evnt.SessionId
        };
        await _client.PublishAsync(_topicName, new[] {message}, CallSettings.FromCancellationToken(ct));
    }
}


namespace Ingest.Api.DTO;

public class EventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CampaignId { get; set; }
    public string? ProductId { get; set; }
    public int? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? SearchTerm { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}
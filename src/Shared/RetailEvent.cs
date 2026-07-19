
namespace Shared;

public class RetailEvent
{
    public Guid EventId {get; set;} = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CampaignId { get; set; }
    public string? ProductId { get; set; }
    public int? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? SearchTerm { get; set; }
    public DateTimeOffset OccuredAt { get; set; } = DateTimeOffset.UtcNow;
}
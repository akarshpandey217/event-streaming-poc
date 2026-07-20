namespace Insights.Api.DTO;

public class CampaignMetricResponse
{
    public string CampaignId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public long UniqueUsers { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
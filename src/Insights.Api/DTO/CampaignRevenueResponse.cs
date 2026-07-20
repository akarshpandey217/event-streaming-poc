using System.Diagnostics;

namespace Insights.Api.DTO;

public class CampaignRevenueResponse
{
    public string CampaignId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
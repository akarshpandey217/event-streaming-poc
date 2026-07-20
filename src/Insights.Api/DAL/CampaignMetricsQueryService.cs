using System.CodeDom;
using Npgsql;
using NpgsqlTypes;
using Shared;

namespace Insights.Api.Querying;

public class CampaignMetricsQueryService
{
    private readonly NpgsqlDataSource _dataSource;

    public CampaignMetricsQueryService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<long> CountUniqueUsersAsync(
        string tenantId,
        string campaignId,
        EventType eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        const string sql = """
        SELECT count(*) FROM retail.campaign_event_users
        WHERE tenant_id = @tenant_id
            AND campaign_id = @campaign_id
            AND event_type = @event_type
            AND (@from is null or first_seen_at > @from)
            AND (@to is null or first_seen_at < @to)
        """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("tenant_id", tenantId);
        cmd.Parameters.AddWithValue("campaign_id", campaignId);
        cmd.Parameters.AddWithValue("event_type", eventType.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("from", NpgsqlDbType.TimestampTz) {Value = (object?)from ?? DBNull.Value});
        cmd.Parameters.Add(new NpgsqlParameter("to", NpgsqlDbType.TimestampTz) {Value = (object?)to ?? DBNull.Value});
        var result = await cmd.ExecuteScalarAsync(ct);
        return (long)(result ?? 0L);
    }

    public async Task<decimal> SumRevenueAsync(
        string tenantId,
        string campaignId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        const string sql = """
        SELECT coalesce(sum(revenue_amount), 0) FROM retail.campaign_purchase_revenue
        WHERE tenant_id = @tenant_id
            AND campaign_id = @campaign_id
            AND (@from is null or first_seen_at >= @from)
            AND (@to is null or first_seen_at < @to)
        """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("tenant_id", tenantId);
        cmd.Parameters.AddWithValue("campaign_id", campaignId);
        cmd.Parameters.Add(new NpgsqlParameter("from", NpgsqlDbType.TimestampTz) {Value = (object?)from ?? DBNull.Value});
        cmd.Parameters.Add(new NpgsqlParameter("to", NpgsqlDbType.TimestampTz) {Value = (object?)to ?? DBNull.Value});
        var result = await cmd.ExecuteScalarAsync(ct);
        return (decimal)(result ?? 0m);
    }
}
using Google.Apis.Auth.OAuth2;
using Npgsql;
using Shared;

namespace Processor.Worker;

public class EventProcessor
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ProcessorOptions _options;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(NpgsqlDataSource dataSource, ProcessorOptions options, ILogger<EventProcessor> logger)
    {
        _dataSource = dataSource;
        _options = options;
        _logger = logger;
    }
    public async Task ProcessAsync(RetailEvent evnt, CancellationToken ct)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await InsertRawEventAsync(connection, transaction, evnt, ct);

        switch (evnt.EventType)
        {
            case EventType.AdImpression:
                await RecordUniqueUserAsync(connection, transaction, evnt.TenantId, evnt.CampaignId!,
                EventType.AdImpression, evnt.UserId, evnt.OccuredAt, ct);
                break;
            case EventType.AdClick:
                await RecordUniqueUserAsync(connection, transaction, evnt.TenantId, evnt.CampaignId!,
                EventType.AdClick, evnt.UserId, evnt.OccuredAt, ct);
                await RecordLastClickAsync(connection, transaction, evnt, ct);
                break;
            case EventType.AddToCart:
                await AttributeToLastClickAsync(connection, transaction, evnt, EventType.ClickToBasket, ct);
                break;
            case EventType.Purchase:
                await AttributePurchaseAsync(connection, transaction, evnt, ct);
                break;
            case EventType.ProductView:
            case EventType.Search:
            case EventType.RemoveFromCart:
            case EventType.Wishlist:
                //stored as raw event only - not part of any campaign metric
                break;
        }
        await transaction.CommitAsync(ct);
    }


    private async Task AttributeToLastClickAsync(NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, EventType attributedEventType, CancellationToken ct)
    {
        var campaignId = await FindAttributedCampaignAsync(connection, tx, evnt, ct);
        if(campaignId is null)
        {
            return;
        }
        await RecordUniqueUserAsync(connection, tx, evnt.TenantId, campaignId, attributedEventType, evnt.UserId, evnt.OccuredAt, ct);
    }

    private async Task AttributePurchaseAsync(NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, CancellationToken ct)
    {
        var campaignId = await FindAttributedCampaignAsync(connection, tx, evnt, ct);
        if(campaignId is null)
        {
            return;
        }
        await RecordUniqueUserAsync(connection, tx, evnt.TenantId, campaignId, EventType.Purchase, evnt.UserId, evnt.OccuredAt, ct);
        var revenue = (evnt.Quantity ?? 1) * (evnt.UnitPrice?? 0m);
        await RecordPurchaseRevenueAsync(connection, tx, evnt, campaignId, revenue, ct);
    }
    private async Task<string?> FindAttributedCampaignAsync(NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, CancellationToken ct)
    {
        const string selectSql = """
        select campaign_id, clicked_at from retail.session_last_click where
        tenant_id = @tenant_id and session_id = @session_id
        """;
        await using var cmd = new NpgsqlCommand(selectSql, connection, tx);
        cmd.Parameters.AddWithValue("tenant_id",evnt.TenantId);
        cmd.Parameters.AddWithValue("campaign_id", evnt.SessionId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if(!await reader.ReadAsync(ct))
        {
            _logger.LogDebug("No recent ad click for the session {SessionId} - {EventType} not attributed to a campaign",
            evnt.SessionId, evnt.SessionId);
            return null;
        }

        var campaignId = reader.GetString(0);
        var clickedAt = reader.GetFieldValue<DateTimeOffset>(1);
        var withinWindow = evnt.OccuredAt - clickedAt <= TimeSpan.FromMinutes(_options.AttributionWindowMinutes);
        if (!withinWindow)
        {
            _logger.LogDebug("Last click for session {SessionId} is outside the {Minutes} min attribution window",
            evnt.SessionId, _options.AttributionWindowMinutes);
            return null;
        }
        return campaignId;
    }

    private static async Task RecordPurchaseRevenueAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, string campaignId, decimal revenue, CancellationToken ct)
    {
        const string sql = """
        insert into retail.campaign_purchase_revenue(tenant_id, campaign_id, event_id, user_id, revenue_amount, occured_at)
        values (@tenant_id, @campaign_id, @event_id, @user_id, @revenue_amount, @occured_at)
        on conflict (tenant_id, campaign_id, event_id) do nothing
        """;

        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("tenant_id",evnt.TenantId);
        cmd.Parameters.AddWithValue("campaign_id", campaignId);
        cmd.Parameters.AddWithValue("event_id",evnt.EventId);
        cmd.Parameters.AddWithValue("user_id",evnt.UserId);
        cmd.Parameters.AddWithValue("revenue_amount",revenue);
        cmd.Parameters.AddWithValue("occured_at",evnt.OccuredAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task RecordLastClickAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, CancellationToken ct)
    {
        const string sql = """
        INSERT into retail.session_last_click (tenant_id, session_id, campaign_id, clicked_at)
        values (@tenant_id, @session_id, @campaign_id, @clicked_at)
        on conflict (tenant_id, session_id) DO UPDATE
            SET campaign_id = excluded.campaign_id, clicked_at = excluded.clicked_at
        """;

        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("tenant_id",evnt.TenantId);
        cmd.Parameters.AddWithValue("session_id", evnt.SessionId);
        cmd.Parameters.AddWithValue("campaign_id",evnt.CampaignId!);
        cmd.Parameters.AddWithValue("occured_at",evnt.OccuredAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task RecordUniqueUserAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, string tenantId, string campaignId, EventType eventType, string userId, DateTimeOffset occurredAt, 
        CancellationToken ct)
    {
        const string sql = """
        Insert into retail.campaign_event_users (tenant_id, campaign_id, event_type, user_id, first_seen_at)
        values (@tenant_id, @campaign_id, @event_type, @user_id, @first_seen_at)
        ON CONFLICT (tenant_id, campaign_id, event_type, user_id) DO NOTHING
        """;

        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("tenant_id", tenantId);
        cmd.Parameters.AddWithValue("campaign_id", campaignId);
        cmd.Parameters.AddWithValue("event_type", eventType.ToString());
        cmd.Parameters.AddWithValue("user_id",userId);
        cmd.Parameters.AddWithValue("first_seen_at", occurredAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    
    private static async Task InsertRawEventAsync(NpgsqlConnection connection, NpgsqlTransaction tx, RetailEvent evnt, CancellationToken ct)
    {
        const string sql = """
        INSERT into retail.raw_events
            (event_id, tenant_id, event_type, session_id, user_id, campaign_id, product_id, quantity, unit_price, search_term, occurred_at)
        VALUES
            (@event_id, @tenant_id, @event_type, @session_id, @user_id, @campaign_id, @product_id, @quantity, @unit_price, @search_term, @occurred_at)
        ON CONFLICT (event_id) DO NOTHING
        """;

        await using var cmd = new NpgsqlCommand(sql, connection, tx);
         cmd.Parameters.AddWithValue("tenant_id", evnt.TenantId);
        cmd.Parameters.AddWithValue("event_type", evnt.EventType.ToString());
        cmd.Parameters.AddWithValue("user_id",evnt.UserId);
        cmd.Parameters.AddWithValue("session_id", evnt.SessionId);
        cmd.Parameters.AddWithValue("occured_at",evnt.OccuredAt);
        cmd.Parameters.AddWithValue("campaign_id",(object?)evnt.CampaignId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("product_id",(object?)evnt.ProductId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("quantity",(object?)evnt.Quantity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("unit_price",(object?)evnt.UnitPrice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("search_term",(object?)evnt.SearchTerm ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);

    }
}

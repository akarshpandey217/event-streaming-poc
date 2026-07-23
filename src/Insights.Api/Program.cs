using Insights.Api.DTO;
using Insights.Api.Querying;
using Npgsql;
using Prometheus;
using Shared;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres") ??
    throw new InvalidOperationException("ConnectionStrings: Postgres is not configured");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<CampaignMetricsQueryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpMetrics();
app.MapMetrics();
app.MapGet("/health", ()=>Results.Ok(new {status = "ok"})).WithName("Health").WithOpenApi();

MapMetricEndpoint(app, "impressions", EventType.AdImpression);
MapMetricEndpoint(app, "clicks", EventType.AdClick);
MapMetricEndpoint(app, "clickToBasket", EventType.ClickToBasket);
MapMetricEndpoint(app, "purchases", EventType.Purchase);

app.MapGet("/ad/{campaignId}/revenue", async (
    string campaignId,
    DateTimeOffset? from,
    DateTimeOffset? to,
    HttpRequest httpReq,
    CampaignMetricsQueryService query,
    CancellationToken ct) =>
{
    var tenantId = httpReq.Headers["X-Tenant-Id"].ToString();
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.BadRequest(new { error = "X-Tenant-Id header is required"});
    }

    var revenue = await query.SumRevenueAsync(tenantId, campaignId, from, to, ct);
    return Results.Ok(new CampaignRevenueResponse
    {
        CampaignId = campaignId,
        Metric = "revenue",
        Revenue = revenue,
        From = from,
        To = to
    });
}).WithOpenApi();

app.Run();

static void MapMetricEndpoint(WebApplication app, string route, EventType eventType)
{
    app.MapGet($"/ad/{{campaignId}}/{route}", async (
        string campaignId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        HttpRequest httpReq,
        CampaignMetricsQueryService queryService,
        CancellationToken ct) =>
    {
        var tenantId = httpReq.Headers["X-Tenant-Id"].ToString();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Results.BadRequest(new { error = "X-Tenant-Id header is required"});
        }

        var count = await queryService.CountUniqueUsersAsync(tenantId, campaignId, eventType, from, to, ct);
        return Results.Ok(new CampaignMetricResponse
        {
            CampaignId = campaignId,
            Metric = route,
            UniqueUsers = count,
            From = from,
            To = to
        });
    }).WithOpenApi();
}

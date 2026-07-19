// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;
var options = ParseArgs(args);

Console.WriteLine($"Simulating {options.Tenants} tenants x"+
$"{options.Campaigns} campaigns at {options.RatePerSecond}/s for "+
$"{options.DurationSeconds}s against {options.CollectorUrl}");


using var http = new HttpClient{BaseAddress = new Uri(options.CollectorUrl)};
var random = new Random();

string[] tenantIds = Enumerable.Range(1, options.Tenants).Select(i => $"tenant-{i}").ToArray();
string[] campaignIds = Enumerable.Range(1, options.Campaigns).Select(i => $"camp-{i}").ToArray();
string[] searchTerms = new[] {"running shoes", "wireless earbuds", "coffee maker", "yoga mat", "laptop stand"};

Dictionary<string, string> recentClicks = new Dictionary<string, string>();

Dictionary<string, string> recentCarts = new Dictionary<string, string>();

var deadline = DateTime.UtcNow.AddSeconds(options.DurationSeconds);
var delayBetweenTicks = TimeSpan.FromMilliseconds(1000/options.RatePerSecond);

int sent = 0;
int failed = 0;

while (DateTime.UtcNow < deadline)
{
    var tenantId = tenantIds[random.Next(tenantIds.Length)];
    var sessionId = $"sess-{random.Next(1,200)}";
    var userId = $"user-{random.Next(1,500)}";
    var body = NextEventBody(sessionId, userId);
    using var request = new HttpRequestMessage(HttpMethod.Post, "/events"){Content = JsonContent.Create(body)};
    request.Headers.Add("X-Tenant-Id", tenantId);
    try
    {
        using var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            sent++;
        }
        else
        {
            failed++;
        }
    }
    catch
    {
        failed++;
    }
    await Task.Delay(delayBetweenTicks);
}

object NextEventBody(string sessionId, string userId)
{
    if(recentCarts.TryGetValue(sessionId, out var productAddedToCart) && random.NextDouble() < 0.4)
    {
        recentCarts.Remove(sessionId);
        return new
        {
            eventType = "Purchase",
            sessionId,
            userId,
            productId = productAddedToCart,
            quantity = random.Next(1,4),
            unitPrice = Math.Round(random.NextDouble() * 90 + 10, 2)
        };
    }
    if(recentClicks.ContainsKey(sessionId) && random.NextDouble() < 0.35)
    {
        var productId = $"sku-{random.Next(1,50)}";
        recentCarts[sessionId] = productId;
        recentClicks.Remove(sessionId);
        return new
        {
            eventType = "AddToCart",
            sessionId,
            userId,
            productId,
            quantity = random.Next(1,3)
        };
    }
    
    var rollbasedEvent = ReturnEventsBasedOnRoll(random.NextDouble(), sessionId, userId, random, searchTerms);
    if(rollbasedEvent != null)
    {
        return rollbasedEvent;
    }
    var campaignId = campaignIds[random.Next(campaignIds.Length)];
    var isClick = random.NextDouble() < 0.03;
    if (isClick)
    {
        recentClicks[sessionId] = campaignId;
    }
    return new
    {
        eventType = isClick ? "AdClick" : "AdImpression",
        sessionId,
        userId,
        campaignId
    };
}

Console.WriteLine($"Done. Sent {sent}, failed {failed}.");
return;

object ReturnEventsBasedOnRoll(double roll, string sessionId, string userId, Random random, string[] searchTerms)
{
    string eventType = string.Empty;
    string productId = $"sku-{random.Next(1,50)}";
    string searchTerm = string.Empty;
    if(roll < 0.30)
    {
        eventType = "ProductView";
    }
    if(roll < 0.40)
    {
        eventType = "Search";
        searchTerm = searchTerms[random.Next(searchTerms.Length)];
    }
    if(roll < 0.45)
    {
        eventType = "Wishlist";
    }
    if(roll < 0.50)
    {
        eventType = "RemoveFromCart";
    }
    if (!string.IsNullOrEmpty(eventType))
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new
            {
                eventType = "ProductView",
                sessionId,
                userId,
                productId
            };
        }
        else
        {
            return new
            {
                eventType = "ProductView",
                sessionId,
                userId,
                searchTerm
            };
        }
    }
    return null;
}

static SimulatorOptions ParseArgs(string[] args)
{
    var options = new SimulatorOptions();
    for(int i =0 ; i< args.Length -1; i++)
    {
        switch (args[i])
        {
            case "--tenants":
                options.Tenants = int.Parse(args[++i]);
                break;
            case "--campaigns":
                options.Campaigns = int.Parse(args[++i]);
                break;
            case "--rate":
                options.RatePerSecond = double.Parse(args[++i]);
                break;
            case "--duration":
                options.DurationSeconds = int.Parse(args[++i]);
                break;
            case "--collector-url":
                options.CollectorUrl = args[++i];
                break;
        }
    }
    return options;
}

class SimulatorOptions
{
    public int Tenants { get; set; } =2;
    public int Campaigns {get; set;} = 3;
    public double RatePerSecond { get; set; } = 10;
    public int DurationSeconds { get; set; } = 60;
    public string CollectorUrl {get; set;} = Environment.GetEnvironmentVariable("INGEST_API_URL") ?? "http://localhost:5080";
}



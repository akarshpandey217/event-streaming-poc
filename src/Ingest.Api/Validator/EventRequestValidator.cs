using Ingest.Api.DTO;
using Shared;

namespace Ingest.Api;

public static class EventRequestValidator
{
    public static string? Validate(EventRequest request, string tenantId, out RetailEvent? evnt)
    {
        evnt = null;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return "X-Tenant-Id header is required";
        }
        if(!Enum.TryParse<EventType>(request.EventType, ignoreCase: true, out var eventType))
        {
            return $"eventType '{request.EventType}' is not recognized";
        }
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return "sessionId is required";
        }
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return "userId is required";
        }
        var needsCampaign = eventType is EventType.AdImpression or EventType.AdClick or EventType.ClickToBasket;
        if(needsCampaign && string.IsNullOrWhiteSpace(request.CampaignId))
        {
            return $"campaignId is required for eventType '{eventType}'";
        }
        var needsProduct = eventType is EventType.ProductView or EventType.AddToCart
            or EventType.RemoveFromCart or EventType.Wishlist or EventType.Purchase;
        if(needsProduct && string.IsNullOrWhiteSpace(request.ProductId))
        {
            return $"productId is required for eventType '{eventType}'";
        }

        if(eventType == EventType.Search && string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return "searchTerm is required for eventType 'Search'";
        }

        if(eventType == EventType.Purchase)
        {
            if(request.Quantity is null or <= 0)
            {
                return "quantity is required and must be greater than zero for eventType 'Purchase'";
            }
            if(request.UnitPrice is null or < 0)
            {
                return "unitPrice is required and must not be negative for eventType 'Purchase'";
            }

        }
        else if(eventType == EventType.AddToCart)
        {
            if(request.Quantity is <= 0)
            {
                return "quantity must be greater than zero";
            }
            if(request.UnitPrice is < 0)
            {
                return "unitPrice must not be negative";
            }
        }
        evnt = new RetailEvent
        {
            TenantId = tenantId,
            EventType = eventType,
            SessionId = request.SessionId,
            UserId = request.UserId,
            CampaignId = request.CampaignId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            SearchTerm = request.SearchTerm,
            OccuredAt = request.OccurredAt ?? DateTimeOffset.UtcNow
        };

        return null;
    }
}
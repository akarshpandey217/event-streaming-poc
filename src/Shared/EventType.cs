using System.Text.Json.Serialization;

namespace Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    ProductView,
    AdImpression,
    AdClick,
    AddToCart,
    ClickToBasket,
    Search,
    RemoveFromCart,
    Wishlist,
    Purchase
}

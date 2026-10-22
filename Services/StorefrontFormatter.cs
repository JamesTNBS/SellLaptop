using System.Globalization;
using Laptop.Models;

namespace Laptop.Services;

/// <summary>Centralizes the store's fixed exchange rate and visitor-facing money formatting.</summary>
public sealed class StorefrontFormatter
{
    // Change this one value when the shop owner changes the displayed exchange rate.
    public const decimal VndPerUsd = 26000m;

    public bool IsVietnamese => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi";
    public string DisplayCurrency => IsVietnamese ? "VND" : "USD";
    public decimal DisplayPriceLimit => IsVietnamese ? 10_000m * VndPerUsd : 10_000m;

    public decimal ToDisplayAmount(decimal amount, string? sourceCurrency)
    {
        var source = NormalizeCurrency(sourceCurrency);
        if (source == DisplayCurrency) return amount;

        var converted = source == "USD" ? amount * VndPerUsd : amount / VndPerUsd;
        return DisplayCurrency == "VND"
            ? Math.Round(converted, 0, MidpointRounding.AwayFromZero)
            : Math.Round(converted, 2, MidpointRounding.AwayFromZero);
    }

    public decimal ToDisplayAmount(CartItem item) => ToDisplayAmount(GetEffectivePrice(item.Price, item.Currency, item.PromotionDiscountPercent), item.Currency);
    public decimal ToDisplayAmount(Product item) => ToDisplayAmount(GetEffectivePrice(item), item.Currency);
    public decimal ToDisplayAmount(OrderItem item) => ToDisplayAmount(item.Price, item.Currency);

    public string Format(decimal sourceAmount, string? sourceCurrency) => FormatDisplay(ToDisplayAmount(sourceAmount, sourceCurrency));
    public string Format(CartItem item) => Format(GetEffectivePrice(item.Price, item.Currency, item.PromotionDiscountPercent), item.Currency);
    public string Format(Product item) => Format(GetEffectivePrice(item), item.Currency);
    public string Format(OrderItem item) => Format(item.Price, item.Currency);

    public string FormatDisplay(decimal displayAmount)
    {
        if (DisplayCurrency == "VND")
        {
            var vietnamese = CultureInfo.GetCultureInfo("vi-VN");
            return $"{Math.Round(displayAmount, 0).ToString("N0", vietnamese)} ₫";
        }

        // The shop uses dots for both grouping and decimal separators on its English storefront.
        return displayAmount.ToString("C", CultureInfo.GetCultureInfo("en-US")).Replace(',', '.');
    }

    public decimal GetActiveDiscountPercent(Product item)
    {
        var now = DateTimeOffset.UtcNow;
        return item.Promotions?
            .Where(promotion => promotion.IsActive && promotion.StartsAtUtc <= now && promotion.EndsAtUtc > now)
            .OrderByDescending(promotion => promotion.DiscountPercent)
            .Select(promotion => promotion.DiscountPercent)
            .FirstOrDefault() ?? 0m;
    }

    public decimal GetEffectivePrice(Product item) =>
        GetEffectivePrice(item.Price, item.Currency, GetActiveDiscountPercent(item));

    public decimal GetEffectivePrice(CartItem item) =>
        GetEffectivePrice(item.Price, item.Currency, item.PromotionDiscountPercent);

    public decimal GetEffectivePrice(decimal basePrice, string? currency, decimal? discountPercent)
    {
        if (discountPercent is not > 0m)
        {
            return basePrice;
        }

        var discounted = basePrice * (100m - discountPercent.Value) / 100m;
        var decimals = NormalizeCurrency(currency) == "VND" ? 0 : 2;
        return Math.Round(discounted, decimals, MidpointRounding.AwayFromZero);
    }

    public string FormatOrderTotal(Order order)
    {
        // New orders already store their displayed total. Older orders remain USD.
        if (NormalizeCurrency(order.Currency) == DisplayCurrency)
            return FormatDisplay(order.TotalAmount);
        return Format(order.TotalAmount, order.Currency);
    }

    public string TranslateCondition(string? condition) => condition switch
    {
        "New" when IsVietnamese => "Mới",
        "Like New" when IsVietnamese => "Như mới",
        "Used - Excellent" when IsVietnamese => "Đã qua sử dụng - Rất tốt",
        "Used - Good" when IsVietnamese => "Đã qua sử dụng - Tốt",
        "Used - Fair" when IsVietnamese => "Đã qua sử dụng - Khá",
        _ => condition ?? string.Empty
    };

    public string TranslateOrderStatus(string? status) => status switch
    {
        "Pending" when IsVietnamese => "Đang chờ",
        "Pending Payment" when IsVietnamese => "Chờ thanh toán",
        "Paid" when IsVietnamese => "Đã thanh toán",
        "Delivered" when IsVietnamese => "Đã giao",
        "Cancelled" when IsVietnamese => "Đã hủy",
        _ => status ?? string.Empty
    };

    public string TranslatePaymentMethod(string? paymentMethod) => paymentMethod switch
    {
        "Cash on Delivery" when IsVietnamese => "Thanh toán khi nhận hàng (COD)",
        "Bank Transfer" when IsVietnamese => "Chuyển khoản ngân hàng",
        _ => paymentMethod ?? string.Empty
    };

    public string TranslateRole(string? role) => role switch
    {
        "Admin" when IsVietnamese => "Quản trị viên",
        "User" when IsVietnamese => "Người dùng",
        _ => role ?? string.Empty
    };

    public static string NormalizeCurrency(string? value) =>
        string.Equals(value, "VND", StringComparison.OrdinalIgnoreCase) ? "VND" : "USD";
}

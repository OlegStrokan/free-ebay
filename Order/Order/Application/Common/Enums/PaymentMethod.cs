using System.Text.Json.Serialization;

namespace Application.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Stripe = 1,
    PayPal = 2,
    BankTransfer = 3,
    CashOnDelivery = 4,
    CreditCard = 5,
    BuyNowPayLater = 6,
}

using Domain.Enums;

namespace Api.Mappers;

internal static class PaymentMethodMapper
{
    public static PaymentMethod FromGrpc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PaymentMethod.Unknown;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "card" => PaymentMethod.Card,
            "credit_card" => PaymentMethod.Card,
            "creditcard" => PaymentMethod.Card,
            "debit_card" => PaymentMethod.Card,
            "debitcard" => PaymentMethod.Card,
            "bank" => PaymentMethod.BankTransfer,
            "bank_transfer" => PaymentMethod.BankTransfer,
            "banktransfer" => PaymentMethod.BankTransfer,
            "wallet" => PaymentMethod.Wallet,
            "apple_pay" => PaymentMethod.Wallet,
            "applepay" => PaymentMethod.Wallet,
            "google_pay" => PaymentMethod.Wallet,
            "googlepay" => PaymentMethod.Wallet,
            "paypal" => PaymentMethod.Wallet,
            _ => PaymentMethod.Unknown,
        };
    }

    public static string ToGrpc(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Card => "card",
            PaymentMethod.BankTransfer => "bank_transfer",
            PaymentMethod.Wallet => "wallet",
            _ => "unknown",
        };
    }
}
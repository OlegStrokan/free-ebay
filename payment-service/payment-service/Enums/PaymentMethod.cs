using System.ComponentModel;

namespace payment_service.Enums;

    public enum PaymentMethod
    {
        [Description("CreditCard")]
        CreditCard,
        [Description("PayPal")]
        PayPal,
        [Description("BankTransfer")]
        BankTransfer,
        [Description("CashOnDelivery")]
        CashOnDelivery,
        [Description("ApplePay")]
        ApplePay,
        [Description("GooglePay")]
        GooglePay,
        [Description("Cryptocurrency")]
        Cryptocurrency
    }

namespace payment_service.Helpers;

public static class StripePaymentIntentHelper
{
    public const string RequiresPaymentMethod = "requires_payment_method";
    public const string RequiresConfirmation = "requires_confirmation";
    public const string RequiresAction = "requires_action";
    public const string Processing = "processing";
    public const string RequiresCapture = "requires_capture";
    public const string Canceled = "canceled";
    public const string Succeeded = "succeeded";

    public static readonly string[] All = new[]
    {
    RequiresPaymentMethod,
    RequiresConfirmation,
    RequiresAction,
    Processing,
    RequiresCapture,
    Canceled,
    Succeeded
    };

    public static bool EqualsStatus(string? status, string expected) =>
        string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
}

using Email.IntegrationTests.Infrastructure;
using Email.IntegrationTests.TestHelpers;
using Email.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Email.IntegrationTests.Services;

[Collection("Integration")]
public sealed class SmtpEmailSenderTests(IntegrationFixture fixture)
{
    private IEmailSender Sender => fixture.Services.GetRequiredService<IEmailSender>();

    private MailHogClient MailHog => new(fixture.MailHogApiBaseUrl);

    [Fact]
    public async Task SendAsync_DeliveredToMailHog_WithCorrectRecipientAndSubject()
    {
        using var mailhog = MailHog;
        await mailhog.DeleteAllAsync();

        await Sender.SendAsync(
            to: "buyer@example.com",
            from: "no-reply@free-ebay.com",
            subject: "Your order has been confirmed",
            htmlBody: "<p>Thank you for your purchase!</p>",
            cancellationToken: CancellationToken.None);

        var message = await mailhog.WaitForMessageAsync(
            m => m.Content.Headers.TryGetValue("To", out var to) &&
                 to.Any(v => v.Contains("buyer@example.com")));

        message.Content.Headers["To"].Should().ContainMatch("*buyer@example.com*");
        message.Content.Headers["Subject"].Should().ContainMatch("*Your order has been confirmed*");
        message.Content.Body.Should().Contain("Thank you for your purchase!");
    }

    [Fact]
    public async Task SendAsync_FallsBackToDefaultFromAddress_WhenFromIsEmpty()
    {
        using var mailhog = MailHog;
        await mailhog.DeleteAllAsync();

        await Sender.SendAsync(
            to: "user@example.com",
            from: "",
            subject: "Verify your email",
            htmlBody: "<p>Click to verify</p>",
            cancellationToken: CancellationToken.None);

        var message = await mailhog.WaitForMessageAsync(
            m => m.Content.Headers.ContainsKey("To"));

        message.Content.Headers["From"].Should().ContainMatch("*no-reply@free-ebay.com*");
    }

    [Fact]
    public async Task SendAsync_SetsIsBodyHtml_BodyPreservedVerbatim()
    {
        using var mailhog = MailHog;
        await mailhog.DeleteAllAsync();

        const string html = "<h1>Password reset</h1><p>Use this <a href=\"https://example.com\">link</a></p>";

        await Sender.SendAsync(
            to: "reset@example.com",
            from: "no-reply@free-ebay.com",
            subject: "Reset your password",
            htmlBody: html,
            cancellationToken: CancellationToken.None);

        var message = await mailhog.WaitForMessageAsync(
            m => m.Content.Headers.TryGetValue("To", out var to) &&
                 to.Any(v => v.Contains("reset@example.com")));

        message.Content.Body.Should().Contain("<h1>Password reset</h1>");
        message.Content.Body.Should().Contain("https://example.com");
    }
}

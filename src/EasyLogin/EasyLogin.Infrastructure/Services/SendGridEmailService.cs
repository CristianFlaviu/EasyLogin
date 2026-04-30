using EasyLogin.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EasyLogin.Infrastructure.Services;

public class SendGridEmailService(IConfiguration config) : IEmailService
{
    private readonly string _apiKey = config["Email__SendGridApiKey"]
        ?? throw new InvalidOperationException("Email__SendGridApiKey is not configured.");
    private readonly string _from = config["Email__From"]
        ?? throw new InvalidOperationException("Email__From is not configured.");

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var client = new SendGridClient(_apiKey);
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(_from),
            new EmailAddress(to),
            subject,
            plainTextContent: null,
            htmlContent: htmlBody);

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"SendGrid returned {(int)response.StatusCode}.");
    }
}

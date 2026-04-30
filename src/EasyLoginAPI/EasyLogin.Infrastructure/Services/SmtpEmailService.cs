using EasyLogin.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace EasyLogin.Infrastructure.Services;

public class SmtpEmailService(IConfiguration config) : IEmailService
{
    private readonly string _host = config["Email__SmtpHost"] ?? "localhost";
    private readonly int _port = int.TryParse(config["Email__SmtpPort"], out var p) ? p : 1025;
    private readonly string? _user = config["Email__SmtpUser"];
    private readonly string? _password = config["Email__SmtpPassword"];
    private readonly string _from = config["Email__From"] ?? "noreply@easylogin.com";

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.None);

        if (!string.IsNullOrWhiteSpace(_user))
            await client.AuthenticateAsync(_user, _password ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

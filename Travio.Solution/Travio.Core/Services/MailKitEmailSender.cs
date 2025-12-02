
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using MailKit.Net.Smtp;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Setting;
using Travio.Core.Contracts.Services;

namespace Travio.Core.Services
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<MailKitEmailSender> _logger;
        // Serilog will be added here soon 

        public MailKitEmailSender(IOptions<EmailSettings> options, ILogger<MailKitEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            msg.To.Add(MailboxAddress.Parse(email));
            msg.Subject = subject;

            var body = new BodyBuilder { HtmlBody = htmlMessage };
            msg.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // connect
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                // authenticate
                if (!string.IsNullOrWhiteSpace(_settings.SmtpUser))
                {
                    await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
                }

                // send
                await client.SendAsync(msg);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw; 
            }
        }
    }
}

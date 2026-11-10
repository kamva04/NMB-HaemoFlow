using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace NMB_HLabSys.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void Send(string to, string subject, string htmlBody)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var from = _config["Email:From"] ?? "noreply@nmbhdl.co.za";
            var password = _config["Email:Password"] ?? string.Empty;
            var displayName = _config["Email:DisplayName"] ?? "NMB-HLabSys";

            // Validate required fields
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("SMTP password is missing. For Gmail, use an App Password (enable 2FA).");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false   // Explicitly override any default
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(from, displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);

            try
            {
                client.Send(msg);
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.ClientNotPermitted)
            {
                throw new InvalidOperationException(
                    "Authentication failed. If using Gmail, enable 2-Step Verification and use an App Password. " +
                    "For other providers, verify your SMTP credentials and security settings.", ex);
            }
        }

        public void SendWithAttachment(string to, string subject, string htmlBody, byte[] pdfBytes, string fileName)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var from = _config["Email:From"] ?? "noreply@nmbhdl.co.za";
            var password = _config["Email:Password"] ?? string.Empty;
            var displayName = _config["Email:DisplayName"] ?? "NMB-HLabSys";

            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("SMTP password is missing. For Gmail, use an App Password (enable 2FA).");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(from, displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);

            // Properly dispose the MemoryStream after sending
            using var ms = new MemoryStream(pdfBytes);
            var attachment = new Attachment(ms, fileName, "application/pdf");
            msg.Attachments.Add(attachment);

            try
            {
                client.Send(msg);
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.ClientNotPermitted)
            {
                throw new InvalidOperationException(
                    "Authentication failed. If using Gmail, enable 2-Step Verification and use an App Password. " +
                    "For other providers, verify your SMTP credentials and security settings.", ex);
            }
            // Attachment and MemoryStream are disposed automatically via 'using' statements
        }
    }
}
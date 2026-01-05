using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SWCAPI.Services
{
    public class OtpEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OtpEmailService> _logger;

        public OtpEmailService(IConfiguration configuration, ILogger<OtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string recipientEmail, string otp)
        {
            try
            {
                _logger.LogInformation("Sending OTP email to: {Recipient}", recipientEmail);

                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "OtpEmail.html");
                string htmlBody;

                if (File.Exists(templatePath))
                {
                    htmlBody = await File.ReadAllTextAsync(templatePath);
                    htmlBody = htmlBody.Replace("{{otp}}", otp);
                }
                else
                {
                    _logger.LogWarning("OTP email template not found. Using fallback.");
                    htmlBody = GetFallbackOtpHtml(otp);
                }

                await SendEmailAsync(recipientEmail, "Your OTP for ZiCORP Login", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}. Error: {Error}", recipientEmail, ex.Message);
                throw;
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Get SMTP settings from configuration
            string smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            int smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
            string fromEmail = _configuration["EmailSettings:SenderEmail"]?.Trim() ?? "noreply@zicorp.in";
            string fromPassword = _configuration["EmailSettings:Password"] ?? "";
            bool enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, _configuration["EmailSettings:SenderName"] ?? "ZiCORP Solutions"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = enableSsl
            };

            await smtpClient.SendMailAsync(mail);
            _logger.LogInformation("OTP email successfully sent to {Email}", toEmail);
        }

        private string GetFallbackOtpHtml(string otp)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>ZiCORP - Secure Login Verification</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background-color: #f8f9fa; line-height: 1.6;'>
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='margin: 0; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='600' style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08); overflow: hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%); padding: 40px 30px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: 600; letter-spacing: -0.5px;'>ZiCORP</h1>
                            <p style='margin: 8px 0 0 0; color: #e0e7ff; font-size: 16px; font-weight: 400;'>Secure Login Verification</p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 50px 40px;'>
                            <div style='text-align: center;'>
                                <h2 style='margin: 0 0 20px 0; color: #1f2937; font-size: 24px; font-weight: 600;'>Your Verification Code</h2>
                                <p style='margin: 0 0 30px 0; color: #6b7280; font-size: 16px; line-height: 1.5;'>
                                    Please use the following One-Time Password (OTP) to complete your login:
                                </p>
                                
                                <!-- OTP Box -->
                                <div style='background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border: 2px solid #0ea5e9; border-radius: 12px; padding: 30px; margin: 30px 0; display: inline-block;'>
                                    <div style='font-size: 42px; font-weight: 700; color: #0369a1; letter-spacing: 8px; font-family: ""Courier New"", monospace;'>{otp}</div>
                                </div>
                                
                                <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; border-radius: 6px; margin: 30px 0; text-align: left;'>
                                    <p style='margin: 0; color: #92400e; font-size: 14px; font-weight: 500;'>
                                        <strong>Security Notice:</strong> This OTP is valid for 10 minutes and can only be used once. Never share this code with anyone.
                                    </p>
                                </div>
                                
                                <p style='margin: 20px 0 0 0; color: #6b7280; font-size: 14px;'>
                                    If you didn't request this code, please ignore this email or contact our support team.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background: #f8fafc; padding: 30px 40px; border-top: 1px solid #e5e7eb;'>
                            <div style='text-align: center;'>
                                <p style='margin: 0 0 10px 0; color: #374151; font-size: 16px; font-weight: 500;'>
                                    <br/>
                                    <strong>ZiCORP Solutions Team</strong>
                                </p>
                                <p style='margin: 10px 0 0 0; color: #9ca3af; font-size: 12px;'>
                                    This is an automated message. Please do not reply to this email.
                                </p>
                                <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #e5e7eb;'>
                                    <p style='margin: 0; color: #6b7280; font-size: 12px;'>
                                        © 2025 ZiCORP Solutions. All rights reserved.
                                    </p>
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}

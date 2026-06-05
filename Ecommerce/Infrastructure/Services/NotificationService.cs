using Application.Configurations;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;
        private readonly MailSettings _mailSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public NotificationService(
            IUnitOfWork unitOfWork,
            ILogger<NotificationService> logger,
            IOptions<MailSettings> mailOptions,
            IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mailSettings = mailOptions?.Value ?? throw new ArgumentNullException(nameof(mailOptions));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        public async Task SendPointsExpiryWarningAsync(Guid userId, int points, DateTime expiryDate)
        {
            try
            {
                var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found. Expiry warning skipped.", userId);
                    return;
                }

                var recipientName = user.FullName;
                var recipientEmail = user.Email;
                var subject = "Your loyalty points will expire soon";

                var htmlBody = await GetEmailBodyAsync("PointsExpiryWarning", new()
                {
                    ["{RecipientName}"] = WebUtility.HtmlEncode(recipientName),
                    ["{Points}"] = points.ToString("N0"),
                    ["{ExpiryDate}"] = expiryDate.ToString("MMMM dd, yyyy"),
                    ["{ShopUrl}"] = "#"
                }, isHtml: true);

                var textBody = await GetEmailBodyAsync("PointsExpiryWarning", new()
                {
                    ["{RecipientName}"] = recipientName,
                    ["{Points}"] = points.ToString("N0"),
                    ["{ExpiryDate}"] = expiryDate.ToString("MMMM dd, yyyy")
                }, isHtml: false);

                if (string.IsNullOrWhiteSpace(_mailSettings.SmtpServer) || string.IsNullOrWhiteSpace(_mailSettings.From))
                {
                    _logger.LogWarning("SMTP settings not configured. Skipped sending expiry warning to {Email}, printed to emails.log instead.", recipientEmail);
                    await AppendEmailLogFileAsync($"To: {recipientEmail}\nSubject: {subject}\n\n{textBody}");
                    return;
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_mailSettings.From, _mailSettings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(recipientEmail, recipientName));

                int smtpPort = 587;
                if (int.TryParse(_mailSettings.Port, out var parsedPort) && parsedPort > 0)
                {
                    smtpPort = parsedPort;
                }

                using var smtpClient = new SmtpClient(_mailSettings.SmtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(_mailSettings.From, _mailSettings.Password),
                    EnableSsl = _mailSettings.EnableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Successfully sent expiry warning to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send expiry warning email to userId {UserId}", userId);
            }
        }

        public async Task SendOrderConfirmationAsync(Order order)
        {
            if (order == null) return;

            try
            {
                var user = order.User;
                if (user == null)
                {
                    user = await _unitOfWork.GetRepository<User>().GetByIdAsync(order.UserId);
                }

                if (user == null)
                {
                    _logger.LogWarning("User not found for order #{OrderCode}. Order confirmation email skipped.", order.OrderCode);
                    return;
                }

                var recipientName = user.FullName;
                var recipientEmail = user.Email;

                var (itemsHtml, itemsText) = await RenderOrderItemsAsync(order);

                var htmlBody = await GetEmailBodyAsync("OrderConfirmation", new()
                {
                    ["{RecipientName}"] = WebUtility.HtmlEncode(recipientName),
                    ["{OrderCode}"] = WebUtility.HtmlEncode(order.OrderCode.ToString()),
                    ["{OrderDate}"] = WebUtility.HtmlEncode(order.CreatedAt.ToString()),
                    ["{PaymentMethod}"] = WebUtility.HtmlEncode(order.PaymentMethod.ToString()),
                    ["{OrderStatus}"] = WebUtility.HtmlEncode(order.Status.ToString()),
                    ["{TotalAmount}"] = WebUtility.HtmlEncode(order.TotalAmount.Amount.ToString("N0")),
                    ["{ItemsHtml}"] = itemsHtml
                }, isHtml: true);

                var textContent = await GetEmailBodyAsync("OrderConfirmation", new()
                {
                    ["{CurrentDate}"] = DateTime.Now.ToString(),
                    ["{RecipientName}"] = recipientName,
                    ["{RecipientEmail}"] = recipientEmail,
                    ["{OrderCode}"] = order.OrderCode.ToString(),
                    ["{OrderDate}"] = order.CreatedAt.ToString(),
                    ["{PaymentMethod}"] = order.PaymentMethod.ToString(),
                    ["{OrderStatus}"] = order.Status.ToString(),
                    ["{TotalAmount}"] = order.TotalAmount.Amount.ToString("N0"),
                    ["{ItemsText}"] = itemsText
                }, isHtml: false);

                if (string.IsNullOrWhiteSpace(_mailSettings.SmtpServer) || string.IsNullOrWhiteSpace(_mailSettings.From))
                {
                    _logger.LogWarning("SMTP settings are not configured. Skipped sending email to {Email}, printed to emails.log instead.", recipientEmail);
                    await AppendEmailLogFileAsync(textContent);
                    return;
                }

                _logger.LogInformation("Sending SMTP email confirmation to {Email} for order #{OrderCode}", recipientEmail, order.OrderCode);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_mailSettings.From, _mailSettings.FromName),
                    Subject = $"Order Confirmation - Order #{order.OrderCode}",
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(recipientEmail, recipientName));

                int smtpPort = 587;
                if (int.TryParse(_mailSettings.Port, out var parsedPort) && parsedPort > 0)
                {
                    smtpPort = parsedPort;
                }

                using var smtpClient = new SmtpClient(_mailSettings.SmtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(_mailSettings.From, _mailSettings.Password),
                    EnableSsl = _mailSettings.EnableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Successfully sent email confirmation to {Email} for order #{OrderCode}", recipientEmail, order.OrderCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order code {OrderCode}", order.OrderCode);
            }
        }

        private async Task AppendEmailLogFileAsync(string emailContent)
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "emails.log");
                await File.AppendAllTextAsync(logPath, emailContent + Environment.NewLine + "--------------------------------------------------" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to append email notification log to file.");
            }
        }

        private async Task<string> GetEmailBodyAsync(string templateName, Dictionary<string, string> replacements, bool isHtml = true)
        {
            var ext = isHtml ? "html" : "txt";
            var fileName = $"{templateName}.{ext}";

            var templatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Templates", fileName);
            if (!File.Exists(templatePath))
            {
                templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", fileName);
            }

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template not found at {Path}", templatePath);
                throw new FileNotFoundException("Email template not found.", templatePath);
            }

            var content = await File.ReadAllTextAsync(templatePath);
            foreach (var (key, value) in replacements)
            {
                content = content.Replace(key, value);
            }
            return content;
        }

        private async Task<(string htmlRows, string textRows)> RenderOrderItemsAsync(Order order)
        {
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                return ("<tr><td colspan=\"3\" style=\"padding: 10px; text-align: center; color: #9ca3af;\">No items found</td></tr>", "  (No items found)");
            }

            var htmlRowTemplatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Templates", "OrderItemRow.html");
            if (!File.Exists(htmlRowTemplatePath))
            {
                htmlRowTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "OrderItemRow.html");
            }

            var textRowTemplatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Templates", "OrderItemRow.txt");
            if (!File.Exists(textRowTemplatePath))
            {
                textRowTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "OrderItemRow.txt");
            }

            if (!File.Exists(htmlRowTemplatePath))
            {
                _logger.LogError("HTML Row template file not found at {Path}", htmlRowTemplatePath);
                throw new FileNotFoundException("HTML Row template file not found.", htmlRowTemplatePath);
            }

            if (!File.Exists(textRowTemplatePath))
            {
                _logger.LogError("Text Row template file not found at {Path}", textRowTemplatePath);
                throw new FileNotFoundException("Text Row template file not found.", textRowTemplatePath);
            }

            var htmlTemplate = await File.ReadAllTextAsync(htmlRowTemplatePath);
            var textTemplate = await File.ReadAllTextAsync(textRowTemplatePath);

            var htmlBuilder = new StringBuilder();
            var textBuilder = new StringBuilder();

            foreach (var item in order.OrderItems)
            {
                var productName = "Product";
                var sku = "";
                var color = "";
                var size = "";

                if (!string.IsNullOrEmpty(item.ProductSnapshot))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(item.ProductSnapshot);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("ProductName", out var nameProp))
                        {
                            productName = nameProp.GetString() ?? productName;
                        }
                        if (root.TryGetProperty("SKU", out var skuProp))
                        {
                            sku = skuProp.GetString() ?? sku;
                        }
                        if (root.TryGetProperty("Color", out var colorProp))
                        {
                            color = colorProp.GetString() ?? color;
                        }
                        if (root.TryGetProperty("Size", out var sizeProp))
                        {
                            size = sizeProp.GetString() ?? size;
                        }
                    }
                    catch
                    {
                    }
                }

                var variantDetails = "";
                if (!string.IsNullOrEmpty(color) || !string.IsNullOrEmpty(size))
                {
                    variantDetails = $" ({string.Join(", ", new[] { color, size }.Where(s => !string.IsNullOrEmpty(s)))})";
                }

                var encodedProductName = WebUtility.HtmlEncode(productName);
                var encodedSku = WebUtility.HtmlEncode(sku);
                var encodedVariantDetails = WebUtility.HtmlEncode(variantDetails);

                var rowHtml = htmlTemplate
                    .Replace("{ProductName}", encodedProductName)
                    .Replace("{VariantDetails}", encodedVariantDetails)
                    .Replace("{SKU}", encodedSku)
                    .Replace("{Quantity}", item.Quantity.ToString())
                    .Replace("{Price}", item.Price.Amount.ToString("N0"));
                htmlBuilder.Append(rowHtml);

                var rowText = textTemplate
                    .Replace("{ProductName}", productName)
                    .Replace("{VariantDetails}", variantDetails)
                    .Replace("{SKU}", sku)
                    .Replace("{Quantity}", item.Quantity.ToString())
                    .Replace("{Price}", item.Price.Amount.ToString("N0"));
                textBuilder.Append(rowText);
            }

            return (htmlBuilder.ToString(), textBuilder.ToString());
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using POS.Application.Commons.Config;
using POS.Application.Interfaces.Services;

namespace POS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmailSettings _emailSettings;

    public EmailService(IUnitOfWork unitOfWork, IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _emailSettings = emailOptions.Value;

        if (string.IsNullOrWhiteSpace(_emailSettings.Host))
            throw new Exception("EmailSettings:Host is not configured.");

        if (_emailSettings.Port <= 0)
            throw new Exception("EmailSettings:Port is not configured.");

        if (string.IsNullOrWhiteSpace(_emailSettings.UserName))
            throw new Exception("EmailSettings:UserName is not configured.");

        if (string.IsNullOrWhiteSpace(_emailSettings.PassWord))
            throw new Exception("EmailSettings:PassWord is not configured.");
    }

    public async Task SendEmail<T>(T data, int templateId, byte[] pdfBytes, string customer, string pdf)
    {
        var template = await _unitOfWork.EmailTemplate.GetByIdAsync(templateId);

        if (template == null || string.IsNullOrWhiteSpace(template.Body) || string.IsNullOrWhiteSpace(template.Subject))
            throw new Exception("No se pudo cargar la plantilla de correo o el asunto.");

        if (string.IsNullOrWhiteSpace(customer))
            throw new ArgumentNullException(nameof(customer), "El correo del cliente no puede ser nulo o vacío.");

        var populatedBody = PopulateTemplate(template.Body, data) ?? string.Empty;
        var populatedSubject = PopulateTemplate(template.Subject, data) ?? "Sin Asunto";

        var email = new MimeMessage();

        email.From.Add(
            MailboxAddress.Parse(_emailSettings.UserName));

        email.To.Add(
            MailboxAddress.Parse(customer));

        if (!string.IsNullOrWhiteSpace(_emailSettings.CC))
        {
            email.Cc.Add(MailboxAddress.Parse(_emailSettings.CC));
        }

        email.Subject = populatedSubject;

        var builder = new BodyBuilder
        {
            HtmlBody = populatedBody
        };

        if (pdfBytes != null && pdfBytes.Length > 0)
        {
            builder.Attachments.Add($"{pdf}.pdf", pdfBytes, ContentType.Parse("application/pdf"));
        }

        email.Body = builder.ToMessageBody();

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            _emailSettings.Host,
            _emailSettings.Port,
            SecureSocketOptions.StartTls);

        await smtpClient.AuthenticateAsync(
            _emailSettings.UserName,
            _emailSettings.PassWord);

        await smtpClient.SendAsync(email);
        await smtpClient.DisconnectAsync(true);
    }

    private string PopulateTemplate<T>(string templateContent, T data)
    {
        var populatedContent = templateContent ?? string.Empty;

        if (data == null)
            return populatedContent;

        foreach (var property in typeof(T).GetProperties())
        {
            var key = "{{" + property.Name + "}}";
            var value = property.GetValue(data)?.ToString() ?? string.Empty;
            populatedContent = populatedContent.Replace(key, value);
        }

        return populatedContent;
    }
}

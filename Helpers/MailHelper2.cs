using Azure;
using MimeKit;
using System.Net.Mail;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using AutomovilClub.Backend.Models;


namespace AutomovilClub.Backend.Helpers
{
    //public class MailHelper2 : IMailHelper215454
    //{
    //    private readonly IConfiguration _configuration;

    //    public MailHelper2(IConfiguration configuration)
    //    {
    //        _configuration = configuration;
    //    }

    //    public Models.Response SendMail(string to, string subject, string body)
    //    {
    //        try
    //        {
    //            var from = _configuration["Mail:From"];
    //            var smtp = _configuration["Mail:Smtp"];
    //            var port = _configuration["Mail:Port"];
    //            var password = _configuration["Mail:Password"];

    //            var message = new MimeMessage();
    //            message.From.Add(MailboxAddress.Parse(from));
    //            message.To.Add(MailboxAddress.Parse(to));
    //            message.Subject = subject;
    //            var bodyBuilder = new BodyBuilder();
    //            bodyBuilder.HtmlBody = body;
    //            message.Body = bodyBuilder.ToMessageBody();

    //            using (var client = new SmtpClient())
    //            {
    //                client.Connect(smtp, int.Parse(port), false);
    //                client.Authenticate(from, password);
    //                client.Send(message);
    //                client.Disconnect(true);
    //            }

    //            return new Models.Response { IsSuccess = true };

    //        }
    //        catch (Exception ex)
    //        {
    //            return new Models.Response
    //            {
    //                IsSuccess = false,
    //                Message = ex.Message,
    //                Result = ex
    //            };
    //        }
    //    }

    //    public Models.Response SendMail(string to, string subject, string body, List<string> attachmentPaths = null, List<(string FileName, byte[] Content)> attachments = null)
    //    {
    //        try
    //        {
    //            var from = _configuration["Mail:From"];
    //            var smtp = _configuration["Mail:Smtp"];
    //            var port = _configuration["Mail:Port"];
    //            var password = _configuration["Mail:Password"];

    //            var message = new MimeMessage();
    //            message.From.Add(MailboxAddress.Parse(from));
    //            message.To.Add(MailboxAddress.Parse(to));
    //            message.Subject = subject;

    //            var bodyBuilder = new BodyBuilder();
    //            bodyBuilder.HtmlBody = body;

    //            // Agregar adjuntos desde rutas de archivos
    //            if (attachmentPaths != null)
    //            {
    //                foreach (var path in attachmentPaths)
    //                {
    //                    bodyBuilder.Attachments.Add(path);
    //                }
    //            }

    //            // Agregar adjuntos desde contenido de archivos
    //            if (attachments != null)
    //            {
    //                foreach (var attachment in attachments)
    //                {
    //                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content);
    //                }
    //            }

    //            message.Body = bodyBuilder.ToMessageBody();

    //            using (var client = new SmtpClient())
    //            {
    //                client.Connect(smtp, int.Parse(port), false);
    //                client.Authenticate(from, password);
    //                client.Send(message);
    //                client.Disconnect(true);
    //            }

    //            return new Models.Response { IsSuccess = true };

    //        }
    //        catch (Exception ex)
    //        {
    //            return new Models.Response
    //            {
    //                IsSuccess = false,
    //                Message = ex.Message,
    //                Result = ex
    //            };
    //        }
    //    }

    //    public Models.Response SendMail(string to, string subject, string body, List<(string FileName, byte[] Content)> attachments = null)
    //    {
    //        try
    //        {
    //            var from = _configuration["Mail:From"];
    //            var smtp = _configuration["Mail:Smtp"];
    //            var port = _configuration["Mail:Port"];
    //            var password = _configuration["Mail:Password"];

    //            var message = new MimeMessage();
    //            message.From.Add(MailboxAddress.Parse(from));
    //            message.To.Add(MailboxAddress.Parse(to));
    //            message.Subject = subject;

    //            var bodyBuilder = new BodyBuilder();
    //            bodyBuilder.HtmlBody = body;

    //            // Agregar adjuntos desde contenido de archivos
    //            if (attachments != null)
    //            {
    //                foreach (var attachment in attachments)
    //                {
    //                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content);
    //                }
    //            }

    //            message.Body = bodyBuilder.ToMessageBody();

    //            using (var client = new SmtpClient())
    //            {
    //                client.Connect(smtp, int.Parse(port), false);
    //                client.Authenticate(from, password);
    //                client.Send(message);
    //                client.Disconnect(true);
    //            }

    //            return new Models.Response { IsSuccess = true };

    //        }
    //        catch (Exception ex)
    //        {
    //            return new Models.Response
    //            {
    //                IsSuccess = false,
    //                Message = ex.Message,
    //                Result = ex
    //            };
    //        }
    //    }

    //}
}

using Azure;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using MailKit.Security;
using System.Net.Mail;


namespace AutomovilClub.Backend.Helpers
{
    public class MailHelper : IMailHelper
    {
        private readonly IConfiguration _configuration;

        public MailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Models.Response SendMail(string to, string subject, string body)
        {
            try
            {
                var from = "notificaciones@automovilclubcr.com";
                var smtp = _configuration["Mail:Smtp"];
                var port = _configuration["Mail:Port"];
                var password = "Aut0m0v1l2024";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Automóvil Club de Costa Rica", from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Cc.Add(MailboxAddress.Parse("info@automovilclubcr.hs-inbox.com"));
                message.Cc.Add(MailboxAddress.Parse("info=automovilclubcr.com@bf.hs-inbox.com"));

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = body;
                
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Deshabilitar la validación del certificado del servidor (no recomendado para producción)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(smtp, int.Parse(port), SecureSocketOptions.SslOnConnect);
                    client.Authenticate(from, password);
                    client.Send(message);
                    client.Disconnect(true);
                }

                return new Models.Response { IsSuccess = true };

            }
            catch (Exception ex)
            {
                return new Models.Response
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = ex
                };
            }
        }

        public Models.Response SendMail(string to, string subject, string body, List<string> attachmentPaths = null, List<(string FileName, byte[] Content)> attachments = null)
        {
            try
            {
                var from = _configuration["Mail:From"];
                var smtp = _configuration["Mail:Smtp"];
                var port = _configuration["Mail:Port"];
                var password = _configuration["Mail:Password"];

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Cc.Add(MailboxAddress.Parse("info@automovilclubcr.hs-inbox.com"));
                message.Cc.Add(MailboxAddress.Parse("info=automovilclubcr.com@bf.hs-inbox.com"));


                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = body;

                // Agregar adjuntos desde rutas de archivos
                if (attachmentPaths != null)
                {
                    foreach (var path in attachmentPaths)
                    {
                        bodyBuilder.Attachments.Add(path);
                    }
                }

                // Agregar adjuntos desde contenido de archivos
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content);
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Deshabilitar la validación del certificado del servidor (no recomendado para producción)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(smtp, int.Parse(port), SecureSocketOptions.StartTls);
                    client.Authenticate(from, password);
                    client.Send(message);
                    client.Disconnect(true);
                }

                return new Models.Response { IsSuccess = true };

            }
            catch (Exception ex)
            {
                return new Models.Response
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = ex
                };
            }
        }


        public Models.Response SendMail(string to, string subject, string body, List<(string FileName, byte[] Content)> attachments = null)
        {
            try
            {
                var from = "notificaciones@automovilclubcr.com";
                var smtp = _configuration["Mail:Smtp"];
                var port = _configuration["Mail:Port"];
                var password = "Aut0m0v1l2024";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Automóvil Club de Costa Rica", from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Cc.Add(MailboxAddress.Parse("info@automovilclubcr.hs-inbox.com"));
                message.Cc.Add(MailboxAddress.Parse("info=automovilclubcr.com@bf.hs-inbox.com"));


                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = body;

                // Agregar adjuntos desde contenido de archivos
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content);
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Deshabilitar la validación del certificado del servidor (no recomendado para producción)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(smtp, int.Parse(port), SecureSocketOptions.SslOnConnect);
                    client.Authenticate(from, password);
                    client.Send(message);
                    client.Disconnect(true);
                }

                return new Models.Response { IsSuccess = true };

            }
            catch (Exception ex)
            {
                return new Models.Response
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    Result = ex
                };
            }
        }


    }
}

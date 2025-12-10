using Azure;
using AutomovilClub.Backend.Models;

namespace AutomovilClub.Backend.Helpers
{
    public interface IMailHelper
    {
        Models.Response SendMail(string to, string subject, string body);
        Models.Response SendMail(string to, string subject, string body, List<string> attachmentPaths = null, List<(string FileName, byte[] Content)> attachments = null);

        Models.Response SendMail(string to, string subject, string body, List<(string FileName, byte[] Content)> attachments = null);
    }
}

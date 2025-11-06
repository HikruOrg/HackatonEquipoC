using System.Net.Mail;
using System.Net;

namespace InterviewCopilotInsight
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string html);
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _from, _host, _user, _pass; private readonly int _port;
        public SmtpEmailSender(string from, string host, int port, string user, string pass)
        { _from = from; _host = host; _port = port; _user = user; _pass = pass; }
        public async Task SendAsync(string to, string subject, string html)
        {
            using var c = new SmtpClient(_host, _port) { EnableSsl = true, Credentials = new NetworkCredential(_user, _pass) };
            var msg = new MailMessage(_from, to, subject, html) { IsBodyHtml = true };
            await c.SendMailAsync(msg);
        }
    }

    public class NoopEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string html)
        {
            Console.WriteLine($"[DEV-EMAIL] To:{to} Subject:{subject}\n{html}");
            return Task.CompletedTask;
        }
    }
}

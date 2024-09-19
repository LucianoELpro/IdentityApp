using Api.DTO.Account;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendEmailAsync(EmailSendDto emailSendDto)
        {

            MailjetClient client = new MailjetClient(_config["MailJet:ApiKey"], _config["MailJet:SecretKey"]);
            var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(_config["Email:From"], _config["Email:ApplicationName"]))
                .WithSubject(emailSendDto.Subject)
                .WithHtmlPart(emailSendDto.Body)
                .WithTo(new SendContact(emailSendDto.To))
                .Build();
            var response = await client.SendTransactionalEmailAsync(email);
            if (response.Messages != null)
            {
                if (response.Messages[0].Status == "success")
                {
                    return true;
                }
            }

            return false;
        }

        //public async Task<bool> SendEmailAsync(EmailSendDto emailSendDto) {

        //    try
        //    {
        //        var userName = _config["SMTP:Username"];
        //        var password = _config["SMTP:Password"];

        //        var client = new SmtpClient("smtp-mail.outlook.com", 587)
        //        {

        //            EnableSsl = true,
        //            Credentials = new NetworkCredential(userName, password)
        //        };

        //        var message = new MailMessage(from: userName, to: emailSendDto.To, subject: emailSendDto.Subject, body: emailSendDto.Body);

        //        message.IsBodyHtml = true;
        //        await client.SendMailAsync(message);
        //        return true;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }

        //}



    }
}

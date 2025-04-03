using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IntwentyDemo.Services
{
    public class CustomEventService : EventService
    {
        private IntwentySettings Settings { get; }
        public CustomEventService(IIntwentyEmailService emailservice, IIntwentySmsService smsservice,IOptions<IntwentySettings> settings, IIntwentyDbLoggerService dblogger) : base(emailservice, smsservice, dblogger)
        {
            Settings = settings.Value;
        }

        public override async Task NewUserCreated(NewUserCreatedData data)
        {
            await base.NewUserCreated(data);

            var mailmsg = string.Format("<br /><br /><b>Thank you for registering an account at {0}</b><br /><br />", Settings.ProductTitle);
            mailmsg += $"Please confirm that this was sent to your email by <a href='{HtmlEncoder.Default.Encode(data.ConfirmCallbackUrl)}'>clicking here</a>";
            await EmailService.SendEmailAsync(data.Email, "Thank you for creating an account.", mailmsg);
        }

        public override async Task EmailChanged(EmailChangedData data)
        {

            await base.EmailChanged(data);

            var mailmsg = string.Format("<br /><br /><b>You updated your email at {0}</b><br /><br />", Settings.ProductTitle);
            mailmsg += $"Please confirm by <a href='{HtmlEncoder.Default.Encode(data.ConfirmCallbackUrl)}'>clicking here</a>";

            await EmailService.SendEmailAsync(data.Email, "Please confirm changed email.", mailmsg);
        }

        public override async Task UserActivatedEmailMfa(UserActivatedEmailMfaData data)
        {
            await base.UserActivatedEmailMfa(data);

            var mailmsg = string.Format("<br /><br /><b>Use this code to activate 'Code To Email' 2FA at {0}</b><br /><br />", Settings.ProductTitle);
            mailmsg += string.Format("Your code is: {0}", data.Code);

            await EmailService.SendEmailAsync(data.Email, "Activate Code To Email 2FA.", mailmsg);
        }

        public override async Task UserActivatedSmsMfa(UserActivatedSmsMfaData data)
        {
            await base.UserActivatedSmsMfa(data);

            var smsmsg = string.Format("Use this code to activate 'Code To SMS' 2FA: {0}", data.Code);

            await SmsService.SendSmsAsync(data.PhoneNumber, smsmsg);
        }

        public override async Task UserRequestedEmailMfaCode(UserRequestedEmailMfaCodeData data)
        {
            await base.UserRequestedEmailMfaCode(data);

            var mailmsg = string.Format("<br /><br />Your requested security code is: <b>{0}</b>", data.Code);

            await EmailService.SendEmailAsync(data.Email, "Your requested security code.", mailmsg);
        }

        public override async Task UserRequestedSmsMfaCode(UserRequestedSmsMfaCodeData data)
        {
            await base.UserRequestedSmsMfaCode(data);

            var smsmsg = string.Format("Your requested security code is: {0}", data.Code);

            await SmsService.SendSmsAsync(data.PhoneNumber, smsmsg);
        }

        public override async Task UserRequestedPasswordReset(UserRequestedPasswordResetData data)
        {
            await base.UserRequestedPasswordReset(data);

            var mailmsg = string.Format("<br /><br /><b>{0} - Password Reset</b><br /><br />", Settings.ProductTitle);
            mailmsg += $"Reset your password by <a href='{HtmlEncoder.Default.Encode(data.ConfirmCallbackUrl)}'>clicking here</a>";
            await EmailService.SendEmailAsync(data.Email, "Password Reset", mailmsg);

        }

    }
}

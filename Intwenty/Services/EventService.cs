using Intwenty.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Services
{
   
    public class EventService : IIntwentyEventService
    {
        protected readonly IIntwentyDbLoggerService DbLoggerService;
        protected readonly IIntwentySmsService SmsService;
        protected readonly IIntwentyEmailService EmailService;


        public EventService(IIntwentyEmailService emailservice, IIntwentySmsService smsservice, IIntwentyDbLoggerService dblogger)
        {
            EmailService = emailservice;
            SmsService = smsservice;
            DbLoggerService = dblogger;
        }

        public virtual async Task NewUserCreated(NewUserCreatedData data) 
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A new user {0} created an account", data.UserName), data.UserName);
        }
        public virtual async Task EmailChanged(EmailChangedData data) 
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} changed registered email to {1}", data.UserName, data.Email), data.UserName);
        }
        public virtual async Task UserActivatedEmailMfa(UserActivatedEmailMfaData data)
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} activates code to email 2FA to {1}", data.UserName, data.Email), data.UserName);
        }
        public virtual async Task UserActivatedSmsMfa(UserActivatedSmsMfaData data)
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} activates sms code 2FA to {1}", data.UserName, data.PhoneNumber), data.UserName);
        }
        public virtual async Task UserRequestedEmailMfaCode(UserRequestedEmailMfaCodeData data)
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} requested a 2FA code via email to {1}", data.UserName, data.Email), data.UserName);
        }
        public virtual async Task UserRequestedSmsMfaCode(UserRequestedSmsMfaCodeData data)
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} requested a 2FA code via SMS to {1}", data.UserName, data.PhoneNumber), data.UserName);
        }
        public virtual async Task UserRequestedPasswordReset(UserRequestedPasswordResetData data)
        {
            await DbLoggerService.LogIdentityActivityAsync("INFO", string.Format("A user {0} requested a password reset email sent to  {1}", data.UserName, data.Email), data.UserName);
        }



    }

   

    public class SenderReceiverUserData
    {
        public string SenderUserName { get; set; }
        public string ReceiverUserName { get; set; }
    }

    public class NewUserCreatedData
    {
       public string UserName { get; set; }
        public string Email { get; set; }
        public string ConfirmCallbackUrl { get; set; }
    }

    public class EmailChangedData
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ConfirmCallbackUrl { get; set; }
    }

    public class UserActivatedEmailMfaData
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
    }

    public class UserActivatedSmsMfaData
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }

    public class UserRequestedEmailMfaCodeData
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
    }

    public class UserRequestedSmsMfaCodeData
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }

    public class UserRequestedPasswordResetData
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ConfirmCallbackUrl { get; set; }
    }

}

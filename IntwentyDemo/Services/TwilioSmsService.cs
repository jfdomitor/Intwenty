using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace IntwentyDemo.Services
{
    
    public class TwilioSmsService : IIntwentySmsService
    {

        private IntwentySettings Settings { get; }


        public TwilioSmsService(IOptions<IntwentySettings> settings)
        {
            Settings = settings.Value;
        }

      
        public async Task<bool> SendSmsAsync(string number, string message)
        {

            if (string.IsNullOrEmpty(Settings.SmsServiceAccountKey) || string.IsNullOrEmpty(Settings.SmsServiceAuthToken) || string.IsNullOrEmpty(number))
                return false;

         
            string tonumber = "";
            if (string.IsNullOrEmpty(Settings.SmsServiceRedirectOutgoingTo))
            {
                tonumber = number.Replace("0046", "+46");
            }
            else
            {
                tonumber = Settings.SmsServiceRedirectOutgoingTo.Replace("0046", "+46");
            }

            TwilioClient.Init(Settings.SmsServiceAccountKey, Settings.SmsServiceAuthToken);

            CreateMessageOptions messageOptions = new CreateMessageOptions(tonumber);
            messageOptions.MessagingServiceSid = Settings.SmsServiceSid;
            messageOptions.Body = message;


            await MessageResource.CreateAsync(messageOptions);

            return true;

        }
    }

}
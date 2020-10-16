﻿using Intwenty;
using Intwenty.Interface;
using Intwenty.SystemEvents;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IntwentyDemo.Services
{
    public class EventService : IntwentySystemEventService
    {


        public EventService(IEmailSender emailsender, IIntwentyDataService dataservice) : base(emailsender, dataservice)
        {
          
        }

        public override void NewUserCreated(NewUserCreatedData data)
        {

            DataService.LogInfo("A new user " + data.UserName + " created an account", username: data.UserName);
            EmailService.SendEmailAsync(data.UserName, "Thank you for creating an account.", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(data.ConfirmCallbackUrl)}'>clicking here</a>");
        }


    }
}

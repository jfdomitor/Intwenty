﻿using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Intwenty;
using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Services;
using Intwenty.WebHostBuilder;
using IntwentyDemo.Services;
using IntwentyDemo.Seed;
using Intwenty.Areas.Identity.Models;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace IntwentyDemo
{
    public class Program
    {

        public static void Main(string[] args)
        {
            try
            {
                var intwentyhost = CreateHostBuilder(args).Build();
                intwentyhost.Run();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddUserSecrets("b77e8d87-d3be-4daf-9074-ec3ccd53ed21");
                    config.AddIntwentyModel();
              
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();

                  

                    webBuilder.ConfigureServices((buildercontext, services) =>
                    {

                        var configuration = buildercontext.Configuration;
                        var settings = configuration.GetSection("IntwentySettings").Get<IntwentySettings>();


                        //****** Required ******
                        //Plug in your own communication service that implements IIntwentyEmailService and IIntwentySmsService
                        //services.TryAddTransient<IIntwentyEmailService, YourEmailService>();
                        //services.TryAddTransient<IIntwentySmsService, YourSmsService>();

                        //Default services
                        services.TryAddTransient<IIntwentyEmailService, EmailService>();
                        services.TryAddTransient<IIntwentySmsService, SmsService>();

                        //****** Required ******
                        //Add intwenty 
                        //Here's where you plug in your own code in intwenty, by overriding esential services.
                        //services.AddIntwenty<CustomDataService, CustomEventService, DemoSeeder, CustomModelService>(configuration);
                        //Default services
                        services.AddIntwenty<EventService>(configuration);


                        //****** Required ******
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy("IntwentyAppAuthorizationPolicy", policy =>
                            {
                                //Anonymus = policy.AddRequirements(new IntwentyAllowAnonymousAuthorization());
                                policy.RequireRole(IntwentyRoles.UserRoles);
                            });

                            options.AddPolicy("IntwentySystemAdminAuthorizationPolicy", policy =>
                            {
                                policy.RequireRole(IntwentyRoles.AdminRoles);

                            });

                        });

                        //****** Required ******
                        //Remove AddRazorRuntimeCompilation() in production
                        services.AddRazorPages().AddViewLocalization().AddRazorRuntimeCompilation();
                        if (settings.AllowBlazor)
                        {
                            services.AddServerSideBlazor().AddCircuitOptions(option => option.DetailedErrors = true);
                        }

                    })
                    .Configure((buildercontext, app) =>
                    {

                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                        var config = app.ApplicationServices.GetRequiredService<IConfiguration>();

                        app.UseStaticFiles();
                        app.UseExceptionHandler("/Home/Error");
                        app.UseHsts();

                        //****** Required ******
                        //Set up everything related to intwenty
                        //Services,routing,endpoints,localization,data seeding and more....
                        app.UseIntwenty();


                    });
                });
                
            /*
                WARNING: Use to turn of dependency injection validation
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateScopes = false;
                    options.ValidateOnBuild = true;
                   
                 });
            */
        }
      

      
    }
}

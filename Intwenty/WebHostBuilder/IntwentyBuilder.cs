using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Localization;
using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Intwenty.DataClient;
using Intwenty.Entity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Intwenty.Seed;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Routing;
using Intwenty.Helpers;

namespace Intwenty.WebHostBuilder
{
    public static class IntwentyBuilder
    {
        public static void AddIntwenty<TIntwentyDataService, TIntwentyEventService>(this IServiceCollection services, IConfiguration configuration)
                         where TIntwentyDataService : class, IIntwentyDataService where TIntwentyEventService : class, IIntwentyEventService
        {
            services.AddIntwenty<TIntwentyDataService, TIntwentyEventService, IntwentySeeder>(configuration);
        }

        public static void AddIntwenty<TIntwentyDataService, TIntwentyEventService, TInwentySeeder>(this IServiceCollection services, IConfiguration configuration)
                         where TIntwentyDataService : class, IIntwentyDataService where TIntwentyEventService : class, IIntwentyEventService where TInwentySeeder : class, IIntwentySeeder
        {
            services.AddIntwenty<TIntwentyDataService, TIntwentyEventService, TInwentySeeder, IntwentyModelService>(configuration);
        }

       public static void AddIntwenty<TIntwentyDataService,TIntwentyEventService,TInwentySeeder,TIntwentyModelService>(this IServiceCollection services, IConfiguration configuration) 
                           where TIntwentyDataService : class, IIntwentyDataService where TIntwentyEventService : class, IIntwentyEventService where TInwentySeeder : class, IIntwentySeeder where TIntwentyModelService : class, IIntwentyModelService
        {
            services.AddSignalR();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var settings = configuration.GetSection("IntwentySettings").Get<IntwentySettings>();

            if (string.IsNullOrEmpty(settings.DefaultConnection))
                throw new InvalidOperationException("Could not find default database connection in setting file");
            if (string.IsNullOrEmpty(settings.IAMConnection))
                throw new InvalidOperationException("Could not find IAM database connection in setting file");
            if (string.IsNullOrEmpty(settings.ProductId))
                throw new InvalidOperationException("Could not find a valid productid in setting file");

            //Create Intwenty database objects
            CreateIntwentyFrameworkTables(settings);
            CreateIntwentyIAMTables(settings);

            //Required for Intwenty: Settings
            services.Configure<IntwentySettings>(configuration.GetSection("IntwentySettings"));

            //Required for Intwenty: Services
            services.TryAddTransient<IIntwentyDbLoggerService, DbLoggerService>();
            services.TryAddTransient<IIntwentyDataService, TIntwentyDataService>();
            services.TryAddTransient<IIntwentyModelService, TIntwentyModelService>();
            services.TryAddTransient<IIntwentyEventService, TIntwentyEventService>();
            services.TryAddTransient<IIntwentyProductManager, IntwentyProductManager>();
            services.TryAddTransient<IIntwentyOrganizationManager, IntwentyOrganizationManager>();
            services.TryAddTransient<IIntwentySeeder, TInwentySeeder>();



            //Required for Intwenty services to work correctly
            services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
                options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

            //Required for Intwenty if Identity is used
            services.AddIdentity<IntwentyUser, IntwentyProductAuthorizationItem>(options =>
            {
                
                options.SignIn.RequireConfirmedAccount = settings.AccountsRequireConfirmed;

                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.User.RequireUniqueEmail = true;

                
                

            })
             .AddRoles<IntwentyProductAuthorizationItem>()
             .AddUserStore<IntwentyUserStore>()
             .AddRoleStore<IntwentyProductAuthorizationStore>()
             .AddUserManager<IntwentyUserManager>()
             .AddSignInManager<IntwentySignInManager>()
             .AddClaimsPrincipalFactory<IntwentyClaimsPricipalFactory>()
             .AddDefaultTokenProviders();

            //If remember me is clicked on sign in, the cookiw will be valid 14 days, otherwise only the session
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.Cookie.Name = "IntwentyAuthCookie";
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Identity/Account/Login";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(365);
                
            });



            if (settings.UseExternalLogins && settings.UseFacebookLogin)
            {
                services.AddAuthentication().AddFacebook(options =>
                {
                    options.AppId = settings.AccountsFacebookAppId;
                    options.AppSecret = settings.AccountsFacebookAppSecret;

                });
            }

            if (settings.UseExternalLogins && settings.UseGoogleLogin)
            {
                services.AddAuthentication().AddGoogle(options =>
                {
                    options.ClientId = settings.AccountsGoogleClientId;
                    options.ClientSecret = settings.AccountsGoogleClientSecret;

                });
            }

            if (settings.UseFrejaEIdLogin)
            {
                services.AddHttpClient<IFrejaClientService, FrejaClientService>()
              .ConfigurePrimaryHttpMessageHandler(() =>
              {
                  HttpClientHandler handler = new HttpClientHandler
                  {
                      AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                      ClientCertificateOptions = ClientCertificateOption.Manual,
                      SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11,
                      ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                  };

                  X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                  certStore.Open(OpenFlags.ReadOnly);
                   X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, settings.FrejaClientCertThumbPrint, false);
                  if (certCollection.Count > 0)
                  {
                      X509Certificate2 cert = certCollection[0];
                      handler.ClientCertificates.Add(cert);
                  }
                  certStore.Close();
                  return handler;
              });
            }
            else
            {
                services.AddHttpClient<IFrejaClientService, FrejaClientService>();
            }

            if (settings.UseBankIdLogin)
            {
               services.AddHttpClient<IBankIDClientService, BankIDClientService>()
              .ConfigurePrimaryHttpMessageHandler(() =>
              {
                  HttpClientHandler handler = new HttpClientHandler
                  {
                      AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                      ClientCertificateOptions = ClientCertificateOption.Manual,
                      SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11,
                      ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                  };

                  X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                  if (!string.IsNullOrEmpty(settings.BankIdCaCertThumbPrint))
                  {
                      certStore.Open(OpenFlags.ReadOnly);
                      X509Certificate2Collection caCertCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, settings.BankIdCaCertThumbPrint, false);
                      if (caCertCollection.Count > 0)
                      {
                          X509Certificate2 cert = caCertCollection[0];
                          //handler.ClientCertificates.Add(cert);
                      }
                  }

                  if (!string.IsNullOrEmpty(settings.BankIdRpCertThumbPrint))
                  {
                      X509Certificate2Collection rpCertCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, settings.BankIdRpCertThumbPrint, false);
                      if (rpCertCollection.Count > 0)
                      {
                          X509Certificate2 cert = rpCertCollection[0];
                          handler.ClientCertificates.Add(cert);
                      }
                  }

                  certStore.Close();
                  return handler;
              });
            }
            else
            {
                services.AddHttpClient<IBankIDClientService, BankIDClientService>();
            }




            if (string.IsNullOrEmpty(settings.LocalizationDefaultCulture))
                throw new InvalidOperationException("Could not find DefaultCulture in setting file");
            if (settings.LocalizationSupportedLanguages == null)
                throw new InvalidOperationException("Could not find SupportedLanguages in setting file");
            if (settings.LocalizationSupportedLanguages.Count == 0)
                throw new InvalidOperationException("Could not find SupportedLanguages in setting file");

            var supportedCultures = settings.LocalizationSupportedLanguages.Select(p => new CultureInfo(p.Culture)).ToList();
            services.Configure<RequestLocalizationOptions>(
                options =>
                {
                    options.AddInitialRequestCultureProvider(new UserCultureProvider());
                    options.DefaultRequestCulture = new RequestCulture(culture: settings.LocalizationDefaultCulture, uiCulture: settings.LocalizationDefaultCulture);
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                    options.RequestCultureProviders.Insert(0, new UserCultureProvider());

                });

            services.AddLocalization();

            services.AddSingleton<IStringLocalizerFactory, IntwentyStringLocalizerFactory>();

            services.AddMvc(options =>
            {
                // This pushes users to login if not authenticated
                options.Filters.Add(new AuthorizeFilter());
                
            }).AddViewLocalization();

            if (settings.APIEnable)
            {
                services.AddSwaggerGen(options =>
                {
                    options.DocumentFilter<APIDocumentFilter>();
                    options.AddSecurityDefinition("API-Key", new OpenApiSecurityScheme
                    {
                        Description = "API-Key",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "API-Key"

                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "API-Key"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                });
            }


        }

        public static IApplicationBuilder UseIntwenty(this IApplicationBuilder builder)
        {
            var configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();
            var settings = configuration.GetSection("IntwentySettings").Get<IntwentySettings>();

            builder.UseHttpsRedirection();
            builder.UseCookiePolicy();

            builder.UseRouting();


            builder.UseAuthentication();
            builder.UseAuthorization();
            builder.UseRequestLocalization();

            builder.ConfigureIntwentyTwoFactorAuth(settings);

            builder.SeedIntwenty(settings);

            builder.ConfigureEndpoints(settings);

            builder.ConfigureSwagger(settings);
            

            return builder;

        }

        private static void ConfigureEndpoints(this IApplicationBuilder builder, IntwentySettings settings)
        {

            builder.UseEndpoints(endpoints =>
            {

                endpoints.MapDefaultControllerRoute();

                //INTWENTY ENDPOINT ROUTING
                using (var scope = builder.ApplicationServices.CreateScope())
                {
                    var serviceProvider = scope.ServiceProvider;
                    var modelservice = serviceProvider.GetRequiredService<IIntwentyModelService>();

                    //REGISTER ENDPOINTS
                    if (settings.APIEnable)
                    {
                        var epmodels = modelservice.GetEndpointModels();

                        foreach (var ep in epmodels)
                        {
                            if (ep.IsMetaTypeCustomPost)
                                continue;
                            if (ep.IsMetaTypeCustomGet)
                                continue;

                            endpoints.MapControllerRoute(ep.MetaCode, ep.Path + "{action=" + ep.Action + "}/{id?}", defaults: new { controller = "DynamicEndpoint" });
                        }
                    }

                    //INTWENTY EXPLICIT APP ROUTING
                    if (settings.StartUpRoutingMode == RoutingModeOptions.Explicit)
                    {                     
                        var appmodels = modelservice.GetApplicationModels();
                        foreach (var a in appmodels)
                        {

                            foreach (var view in a.Views)
                            {
                                if (string.IsNullOrEmpty(view.Path))
                                    continue;

                                var path = view.Path.Trim();
                                if (!path.EndsWith("/"))
                                    path = path + "/";

                                var lbc = path.Count(p => p == '{');
                                var lbr = path.Count(p => p == '}');

                                if (lbc == lbr)
                                {
                                    //View Paths in the model will never be mapped, so default values will be used
                                    endpoints.MapControllerRoute("app_route_" + a.Application.MetaCode + "_" + view.MetaCode, path, defaults: new { controller = "Application", action = "View" });
                                }
                                else
                                {
                                    //
                                }
                                
                            }
                        }
                    }

                   
                }


                if (settings.StartUpRoutingMode == RoutingModeOptions.TakeAll)
                {
                    //INTWENTY APP ROUTING
                    //Applicationpath and viewpath will (probably) never be mapped, so default values will be used
                  
                    endpoints.MapControllerRoute("runtime_routes",
                        "{applicationpath}/{viewpath}/{id:int?}/{requestinfo?}",
                        defaults: new { controller = "Application", action = "View" },
                        constraints: new { applicationpath = "^(?!model|identity|swagger|api)([a-z0-9]+)$", viewpath = "^(?!iam|api|model)([a-z0-9]+)$" });
                }

                endpoints.MapRazorPages();
                endpoints.MapHub<Intwenty.PushData.ServerToClientPush>("/serverhub");


                /* Handle all responses
                endpoints.MapGet("/{**slug}", async context =>
                {
                   await context.Response.WriteAsync("Hello World!");
                });
                */

            });

           
        }

        private static void ConfigureIntwentyTwoFactorAuth(this IApplicationBuilder builder, IntwentySettings settings)
        {

            if (settings.TwoFactorForced)
            {
                builder.UseMiddleware<Forced2FaMiddleware>();
            }

        }

        private static void ConfigureSwagger(this IApplicationBuilder builder, IntwentySettings settings)
        {
            if (settings.APIEnable)
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                builder.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                // specifying the Swagger JSON endpoint.
                builder.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Intwenty API V1");
                });
            }

        }


        public static void SeedIntwenty(this IApplicationBuilder builder, IntwentySettings settings)
        {

            //Below can be activated/deactivated in the appsetting.json file
            //-SeedProductAndOrganizationOnStartUp
            //-UseDemoSettings
            //-SeedModelOnStartUp
            //-SeedLocalizationsOnStartUp
            //-ConfigureDatabaseOnStartUp
            //-SeedDataOnStartUp

            using (var scope = builder.ApplicationServices.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                var seederservice = serviceProvider.GetRequiredService<IIntwentySeeder>();

                //The order is important

                if (settings.StartUpSeedLocalizations)
                {
                    seederservice.SeedLocalization();
                }
                if (settings.StartUpSeedProductAndOrganization || settings.StartUpSeedDemoUserAccounts)
                {
                    seederservice.SeedProductAndOrganization();
                }
                if (settings.StartUpSeedDemoUserAccounts)
                {
                    seederservice.SeedUsersAndRoles();
                }
                if (settings.StartUpSeedModel)
                {
                    seederservice.SeedModel();
                }
                if (settings.StartUpSeedData)
                {
                    seederservice.SeedData();
                }
                if (settings.StartUpConfigureDatabase)
                {
                    seederservice.ConfigureDataBase();
                }
                if (settings.StartUpSeedProductAndOrganization)
                {
                    seederservice.SeedProductAuthorizationItems();
                }
            }
        }

        private static void CreateIntwentyFrameworkTables(IntwentySettings settings)
        {

            if (!settings.StartUpIntwentyDbObjects)
                return;

            var client = new Connection(settings.DefaultConnectionDBMS, settings.DefaultConnection);

            try
            {
                client.Open();
                client.ModifyTable<SystemItem>();
                client.ModifyTable<ApplicationItem>();
                client.ModifyTable<DatabaseItem>();
                client.ModifyTable<EventLog>();
                client.ModifyTable<InformationStatus>();
                client.ModifyTable<InstanceId>();
                client.ModifyTable<ViewItem>();
                client.ModifyTable<UserInterfaceStructureItem>();
                client.ModifyTable<UserInterfaceItem>();
                client.ModifyTable<FunctionItem>();
                client.ModifyTable<ValueDomainItem>();
                client.ModifyTable<DefaultValue>();
                client.ModifyTable<TranslationItem>();
                client.ModifyTable<EndpointItem>();
                client.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                client.Close();
            }



        }

        private static void CreateIntwentyIAMTables(IntwentySettings settings)
        {

            if (!settings.StartUpIntwentyDbObjects)
                return;

            var client = new Connection(settings.IAMConnectionDBMS, settings.IAMConnection);

            try
            {


                client.Open();
                client.ModifyTable<IntwentyAuthorization>(); //security_Authorization
                client.ModifyTable<IntwentyUser>(); //security_User
                client.ModifyTable<IntwentyOrganization>(); //security_Organization
                client.ModifyTable<IntwentyOrganizationMember>(); //security_OrganizationMembers
                client.ModifyTable<IntwentyOrganizationProduct>(); //security_OrganizationProducts
                client.ModifyTable<IntwentyProduct>(); //security_Product
                client.ModifyTable<IntwentyProductAuthorizationItem>(); //security_ProductAuthorizationItem
                client.ModifyTable<IntwentyProductGroup>(); //security_ProductGroup
                client.ModifyTable<IntwentyUserProductGroup>(); //security_UserProductGroup
                client.ModifyTable<IntwentyUserProductClaim>(); //security_UserProductClaim
                client.ModifyTable<IntwentyUserProductLogin>(); //security_UserProductLogin
                client.ModifyTable<IntwentyUserSetting>(); //security_UserSetting
                client.ModifyTable<EventLog>();
                //client.CreateTable<IntwentyProductRoleClaim>(true, true); //security_RoleClaims
                //client.CreateTable<IntwentyUserProductToken>(true, true); //security_UserTokens
                client.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                client.Close();
            }

        }

      


    }
}

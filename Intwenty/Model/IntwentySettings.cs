﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Intwenty.Model
{

    public enum DBMS { MSSqlServer, MySql, MariaDB, PostgreSQL, SQLite, MongoDb, LiteDb };

    public enum LocalizationMethods { SiteLocalization, UserLocalization };


    public class IntwentySettings
    {
        public string DefaultConnection { get; set; }
        public DBMS DefaultConnectionDBMS { get; set; }
        public bool ReCreateDatabaseOnStartup { get; set; }

        public bool SeedDatabaseOnStartUp { get; set; }

        public bool UseDemoSettings { get; set; }

        public string DemoAdminUser { get; set; }
        public string DemoAdminPassword { get; set; }
        public string DemoUser { get; set; }
        public string DemoUserPassword { get; set; }


        /// <summary>
        /// Enable localization
        /// </summary>
        public bool EnableLocalization { get; set; }
        /// <summary>
        /// SiteLocalization = Always use DefaultCulture to look up localization keys
        /// UserLocalization = Always use UserCulture to  look up localization keys
        /// </summary>
        public LocalizationMethods LocalizationMethod { get; set; }
        public string DefaultCulture { get; set; }
        public List<IntwentyLanguage> SupportedLanguages { get; set; }


        /// <summary>
        /// The title of the site where intwenty is used
        /// </summary>
        public string SiteTitle { get; set; }


        public bool EnableExternalLogins { get; set; }
        public bool EnableEMailVerivication { get; set; }

        /// <summary>
        /// The title to show in authenticator apps
        /// </summary>
        public string AuthenticatorTitle { get; set; }
        public bool EnableTwoFactorAuthentication { get; set; }
        public bool ForceTwoFactorAuthentication { get; set; }


        public bool EnableAPIKeyGeneration { get; set; }


        /// <summary>
        /// if true new users can create organization accounts and invite others to be member users, or users can join an aorganization
        /// Users can ask an organization administrator to join an organization
        /// An organization administartor can accept or reject user requests
        /// An organization administrator can invite users to the organization
        /// </summary>
        public bool EnableUserInvites { get; set; }
     

        //EMAIL
        public string MailServiceServer { get; set; }

        public int MailServicePort { get; set; }

        public string MailServiceUser { get; set; }

        public string MailServicePwd { get; set; }

        public string MailServiceAPIKey { get; set; }

        public string MailServiceFromEmail { get; set; }

        //FOR DEBUG MODE
        public string RedirectAllOutgoingMailTo { get; set; }

        //FOR DEBUG MODE
        public string RedirectAllOutgoingSMSTo { get; set; }

        public bool StorageUseFileSystem { get; set; }

        public bool StorageUseStorageAccount { get; set; }

        //AZURE STORAGE
        public string StorageContainer { get; set; }

        public string StorageConnectionString { get; set; }

        public bool IsNoSQL
        {

            get
            {
                if (DefaultConnectionDBMS == DBMS.MongoDb  || DefaultConnectionDBMS == DBMS.LiteDb)
                    return true;

                return false;
            }

        }

      

    }

    public class IntwentyLanguage
    {
        public string Name { get; set; }

        public string Culture { get; set; }
    }
}

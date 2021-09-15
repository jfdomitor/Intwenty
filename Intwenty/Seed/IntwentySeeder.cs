using Intwenty.Areas.Identity.Data;
using Intwenty.Areas.Identity.Entity;
using Intwenty.Areas.Identity.Models;
using Intwenty.DataClient;
using Intwenty.Entity;
using Intwenty.Interface;
using Intwenty.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Seed
{
    public class IntwentySeeder : IIntwentySeeder
    {
        protected IntwentyUserManager UserManager { get; }
        protected RoleManager<IntwentyProductAuthorizationItem> RoleManager { get; }
        protected IIntwentyProductManager ProductManager { get; }
        protected IIntwentyOrganizationManager OrganizationManager { get; }
        protected IIntwentyModelService ModelService { get; }
        protected IntwentySettings Settings { get; }

        public IntwentySeeder(IOptions<IntwentySettings> settings, IntwentyUserManager usermanager, RoleManager<IntwentyProductAuthorizationItem> rolemanager, IIntwentyProductManager productmanager, IIntwentyOrganizationManager orgmanager, IIntwentyModelService modelservice)
        {
            Settings = settings.Value;
            UserManager = usermanager;
            RoleManager = rolemanager;
            ProductManager = productmanager;
            OrganizationManager = orgmanager;
            ModelService = modelservice;
        }

        public virtual void SeedLocalization()
        {
           
            var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);

    
            var temp = new List<TranslationItem>();

            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "LOCKED_OUT_ACCOUNT", Text = "This account has been locked out, please try again later." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "LOCKED_OUT_ACCOUNT", Text = "Det här kontot har spärrats, försök igen senare eller kontakta systemansvarig" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "API Key Updated", Text = "API nyckeln uppdaterades" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You must type a user name and a password", Text = "Vänligen ange användarnamn och lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Invalid login attempt", Text = "Inloggningen misslyckades" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "LOGIN_REQUIRE_CONF", Text = "Your account must be confirmed before you can login, pleas check your email and follow the instructions." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "LOGIN_REQUIRE_CONF", Text = "Du måste bekräfta ditt konto innan du kan logga in, vänligen kontrollera din epost" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "FREJA_SERVICE_FAILURE", Text = "Sorry, the service is temporary unavailable" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "FREJA_SERVICE_FAILURE", Text = "Vi ber om ursäkt, men det går inte att logga in för tillfället" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "FREJA_INSTRUCTION", Text = "To login, open your Freja e ID app and scan the QR code below" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "FREJA_INSTRUCTION", Text = "För att logga in öppnar du appen Freja eID på din telefon och skannar sedan QR koden nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_INSTRUCTION", Text = "To login, open your Bank ID app and scan the QR code below" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_INSTRUCTION", Text = "För att logga in öppnar du Bank ID appen på din telefon och skannar sedan QR koden nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_SERVICE_FAILURE", Text = "Authentication failed" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_SERVICE_FAILURE", Text = "Autentiseringen misslyckades, försök igen" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_CREATE_ACCOUNT", Text = "Welcome. Please input the requested information below and then click 'Register'. You will need your Bank ID to finnish the registration." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_CREATE_ACCOUNT", Text = "Välkommen ! Börja med att ange efterfrågad information och klicka sedan på registreara. Du kommer behöva Bank ID för att slutföra registreringen." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_USE_OTHER_DEVICE", Text = "Bank ID on another device" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_USE_OTHER_DEVICE", Text = "Bank ID på annan enhet" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_USE_THIS_DEVICE", Text = "Bank ID on this device" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_USE_THIS_DEVICE", Text = "Bank ID på denna enhet" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_OPEN_THIS_DEVICE", Text = "Open Bank ID" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_OPEN_THIS_DEVICE", Text = "Öppna Bank ID" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_AUTH_FAILURE", Text = "The BankID you are trying to use is revoked or too old. Please use another BankID or order a new one from your internet bank." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_AUTH_FAILURE", Text = "Det BankID du försöker använda är för gammalt eller spärrat. Använd ett annat BankID eller hämta ett nytt hos din internetbank." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_INTWENTY_TIMEOUT_FAILURE", Text = "You reached the authentication time limit, please try again." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_INTWENTY_TIMEOUT_FAILURE", Text = "Tidsgränsen uppnåddes, vg försök igen" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_TIMEOUT_FAILURE", Text = "The BankID app is not responding. Please check that the program is started and that you have internet access.If you don’t have a valid BankID you can get one from your bank. Try again." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_TIMEOUT_FAILURE", Text = "BankID-appen svarar inte. Kontrollera att den är startad och att du har internetanslutning.Om du inte har något giltigt BankID kan du hämta ett hos din Bank. Försök sedan igen." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_CANCEL_FAILURE", Text = "Action cancelled. Try Again." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_CANCEL_FAILURE", Text = "Åtgärden avbruten. Försök igen." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BANKID_USERCANCEL_FAILURE", Text = "Action cancelled." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BANKID_USERCANCEL_FAILURE", Text = "Åtgärden avbruten." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "RESETPWDMAIL", Text = "Vi har skickat en länk till din epostadress, klicka på den för att återställa ditt lösenord." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "RESETPWDMAIL", Text = "Please check your email to reset your password." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Reset password confirmation", Text = "Bekräftelse på återställt lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Your password has been reset", Text = "Ditt lösenord har återställts" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Reset Password", Text = "Återställ Lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Access Denied", Text = "Du saknar behörighet för den begärda informationen" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFALOGINSMS", Text = "Vi har skickat en kod till din telefon, ange koden och klicka på logga in" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFALOGINSMS", Text = "We've sent a code to your phone, please enter the code and click login" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFALOGINEMAIL", Text = "Vi har skickat en kod till den registrerade epostadress, ange koden och klicka på logga in" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFALOGINEMAIL", Text = "We've sent a code to your registered email address, please enter the code and click login" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFALOGINTOTP", Text = "Ditt konto använder en autensierings app, ange koden från appen och klicka på logga in" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFALOGINTOTP", Text = "Your login is protected with an authenticator app. Enter your authenticator code and click login" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Register", Text = "Registrera " });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create Account", Text = "Skapa Konto" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create a new account", Text = "Skapa nytt konto" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Passwords must match", Text = "Lösenorden skiljer sig åt" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Sorry, registration is closed", Text = "Tjänsten är stängd för registrering" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enter your email", Text = "Ange din epostadress" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPUNVERIFIED", Text = "Koden kunde inte verifiera testa nästa kod i din app." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPUNVERIFIED", Text = "The code could not be verified, try the next code from your authenticator app." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPSTEPS_1", Text = "För att använda en autentiserings app, går du igenom följande steg:" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPSTEPS_1", Text = "To use an authenticator app go through the following steps:" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPSTEPS_2", Text = "Ladda ned en autentiserings app (Microsoft Authenticator or Google Authenticator) till din mobiltelefon." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPSTEPS_2", Text = "Download an authentication app (Microsoft Authenticator or Google Authenticator) to your phone." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPSTEPS_3", Text = "Skanna QR koden nedan eller ange denna kod" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPSTEPS_3", Text = "Scan the QR Code or enter this key" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPSTEPS_4", Text = "i din autentiserings app i telefonen" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPSTEPS_4", Text = "into your two factor authenticator app" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFATOTPSTEPS_5", Text = "När du skannat qr-koden eller matat in koden kommer din autentiserings app att ge dig en unik kod, ange den koden i rutan nedan och klicka på verifiera." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFATOTPSTEPS_5", Text = "Once you have scanned the QR code or input the key above, your two factor authentication app will provide you with a unique code.Enter the code in the confirmation box below." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFASMSCODESENT", Text = "Vi har skickat en kod till ditt telefonnummer, vänligen ange det nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFASMSCODESENT", Text = "We've sent a code to your phone, input the code below." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFASMSPHONEREQ", Text = "Vänligen ange det telefonnummer du vill använda ör att ta emot koder." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFASMSPHONEREQ", Text = "Please input the phonenumber to use with sms 2FA." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Verify", Text = "Verifiera" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Invalid format", Text = "Ogiltigt format" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You must update your email first, do it by", Text = "Du måste ange en epostadress först, det gär du genom att" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Thank you. Two-factor authentication is now set up", Text = "Tack 2-faktor autentisering är nu aktiverad." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFANOTVERIFIED", Text = "Koden kunde inte verifieras, du kan försöka igen genom att" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFANOTVERIFIED", Text = "The code could not be verified, try again by" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "clicking here", Text = "Klicka här" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFAEMALCODESENT", Text = "Vi har skickat en kod till din epostadress, vänligen mata in koden nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFAEMALCODESENT", Text = "We've sent a code to your email, please input that code and push verify." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Reset 2FA", Text = "Återställ 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Disable 2FA", Text = "Avaktivera 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFARESETINFO", Text = "Denna åtgärd tar bort dina 2FA inställningar. För att bibehålla en hög säkerhet bör du aktivera 2FA igen senare" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFARESETINFO", Text = "This action disables your 2FA Settings. To achive the highest security you should set it up again later." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFAFORCEDRESETINFO", Text = "Denna åtgärd tar bort dina 2FA inställningar. Eftersom vår tjänst använder tvingande 2FA måste du göra om konfigurationen för att kunna logga in. Om du använder en app måste du även ta bort kontot i appen och skapa ett nytt." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFAFORCEDRESETINFO", Text = "This action resets your 2FA Settings. Since our service is using enforced 2FA you must configure it again in order to login." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You're using Bank ID 2FA", Text = "Du använder 2FA via Bank ID" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You're using Email 2FA", Text = "Du använder 'Kod via Epost' 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You're using FIDO2 2FA", Text = "Du använder FIDO2 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You're using SMS Code 2FA", Text = "Du använder 'Kod via SMS' 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You're using an authentication app for 2FA", Text = "Du använder 2FA via en autentiserings app" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFASTATUS", Text = "Du är klar. När du loggar in kommer du efterfrågas en extra säkerhetskod som levereras till dig på det sätt du valt." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFASTATUS", Text = "You're done. Next time you login you will be promted for a security code." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable Email Code 2FA", Text = "'Kod via Epost' 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable FIDO2 2FA", Text = "'FIDO2' 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable SMS Code 2FA", Text = "'Kod via SMS' 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable Bank ID 2FA", Text = "2FA via Bank ID" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable Microsoft Authenticator", Text = "Microsoft Authenticator 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Enable Google Authenticator", Text = "Google Authenticator 2FA" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFAOPTIONAL", Text = "Genom att aktivera två-faktor autentisering så höjer du säkerheten på ditt konto, vänligen välj ett av alternativen nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFAOPTIONAL", Text = "By adding two-factor authentication you can add extra security to your account. Please choose one option below." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "MFAFORCED", Text = "För att nå en hög säkerhet använder vi tvingande 2-faktor autentisering. För att använda tjänsten måste du välja ett alternativ nedan." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "MFAFORCED", Text = "To add extra security, our service is using enforced 2FA. To use the service you must enable one of the options below." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Do you want to rename", Text = "Vill du byta namn på" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You must type a new groupname", Text = "Du måste ange ett nytt gruppnamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Invite member", Text = "Bjud in medlem" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "You must type a valid email", Text = "Du måste ange en korrekt epostadress" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "New Groupname", Text = "Nytt gruppnamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Type a new groupname for", Text = "Skriv ett nytt gruppnamn för" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "GROUPINVITESENT", Text = "An inbjudan att delta i gruppen har skickats till" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "GROUPINVITESENT", Text = "An invite to join the group has been sent to" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "GROUPINVITEINSTRUCT", Text = "Skriv epostadressen till en person att bjuda in till" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "GROUPINVITEINSTRUCT", Text = "Type the email of a person to invite to" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "GROUPREQTOOWNERSENT", Text = "En förfrågan om att bli medlem har skickats till gruppens ägare." });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "GROUPREQTOOWNERSENT", Text = "A request to join the group has been sent to the owner." });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Become a member of the group", Text = "Bli medlem i gruppen" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Do you want to create the group", Text = "Vill du skapa gruppen" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Group", Text = "Grupp" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create or Join", Text = "Skapa eller gå med" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Groupname", Text = "Gruppnamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create Group", Text = "Skapa grupp" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Join Group", Text = "Gå med i grupp" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "My Groups", Text = "Mina grupper" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "My role", Text = "Min roll" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Accept", Text = "Acceptera" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Members", Text = "Medlemmar" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Members of my groups", Text = "Medlemmar i mina grupper" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Role", Text = "Roll" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Members in", Text = "Medlemmar i" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Change Password", Text = "Ändra lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Current Password", Text = "Nuvarande lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "New Password", Text = "Nytt lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Confirm Password", Text = "Bekräfta lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Account settings", Text = "Kontoinställningar" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Manage Account", Text = "Hantera ditt konto" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create Key", Text = "Skapa nyckel" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Renew Key", Text = "Förnya nyckel" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "First Name", Text = "Förnamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Last Name", Text = "Efternamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Profile", Text = "Mina uppgifter" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Language", Text = "Språk" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Two-factor authentication", Text = "2-faktor autentisering" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "API Key", Text = "API nyckel" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Groups", Text = "Grupper" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Personal data", Text = "Personlig data" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Use external accounts", Text = "Använd ett externt konto" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Register as a new user", Text = "Skapa konto" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Forgot your password", Text = "Glömt lösenordet" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Remember Me", Text = "Kom i håg mig" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Password", Text = "Lösenord" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "User Name", Text = "Användarnamn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Logout", Text = "Logga Ut" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Login", Text = "Logga In" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Code", Text = "Kod" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Address", Text = "Adress" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Message", Text = "Meddelande" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Email", Text = "Epost" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Phone", Text = "Telefon" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Previous", Text = "Föregående" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Next", Text = "Nästa" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Changed By", Text = "Ändrad Av" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Changed", Text = "Ändrad" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "List", Text = "Lista" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Add", Text = "Lägg till" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Open", Text = "Öppna" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Close", Text = "Stäng" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Title", Text = "Titel" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "Delete", Text = "Delete ?" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Remove", Text = "Ta bort" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Delete", Text = "Ta bort ?" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Cancel", Text = "Avbryt" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Date", Text = "Datum" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Event", Text = "Händelse" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Leave", Text = "Lämna" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Name", Text = "Namn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Menu", Text = "Meny" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Rename", Text = "Ändra namn" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Create new", Text = "Skapa ny" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "New", Text = "Ny" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Save", Text = "Spara" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Changes Saved", Text = "Ändringar sparade" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Edit", Text = "Ändra" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "Send", Text = "Skicka" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "to", Text = "till" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "BACKTOLIST", Text = "Back to list" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "BACKTOLIST", Text = "Tillbaka" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "ADDEDIT", Text = "Add / Edit" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "ADDEDIT", Text = "Lägg till / Ändra" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "COPYRIGHT", Text = "2021 Intwenty - All rights reserved" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "COPYRIGHT", Text = "2021 Intwenty - Alla rättighter reserverade" });
            temp.Add(new TranslationItem() { Culture = "en-US", TransKey = "PRODUCT_LOGIN", Text = "Welcome ! Follow the instructions below to login" });
            temp.Add(new TranslationItem() { Culture = "sv-SE", TransKey = "PRODUCT_LOGIN", Text = "Välkommen ! Följ instruktionerna nedan för att logga in" });

            client.Open();

            var existing = client.GetEntities<TranslationItem>();

            foreach (var t in temp)
            {
                if (!existing.Exists(p => p.Culture == t.Culture && p.TransKey == t.TransKey))
                {
                      client.InsertEntity(t);
                }
                
            }

            client.Close();

        }

        public virtual void SeedProductAndOrganization()
        {

            var t = Task.Run(async () =>
            {
                IntwentyProduct product = await ProductManager.FindByIdAsync(Settings.ProductId);
                if (product == null)
                {
                    product = new IntwentyProduct();
                    product.Id = Settings.ProductId;
                    product.ProductName = Settings.ProductTitle;
                    await ProductManager.CreateAsync(product);
                }

                IntwentyOrganization org = await OrganizationManager.FindByNameAsync(Settings.ProductOrganization);
                if (org == null)
                {
                    org = new IntwentyOrganization();
                    org.Name = Settings.ProductOrganization;
                    await OrganizationManager.CreateAsync(org);
                }

                var all_products = await OrganizationManager.GetProductsAsync(org.Id);
                var thisproduct = all_products.Find(p => p.ProductId == product.Id);
                if (thisproduct == null)
                {
                    await OrganizationManager.AddProductAsync(new IntwentyOrganizationProduct() { OrganizationId = org.Id, ProductId = product.Id, ProductName = product.ProductName });
                }
            });

            t.GetAwaiter().GetResult();
        }


        public virtual void SeedProductAuthorizationItems()
        {
            
            var client = new Connection(Settings.DefaultConnectionDBMS, Settings.DefaultConnection);

            client.Open();
            var current_apps = client.GetEntities<ApplicationItem>();
            var current_systems = client.GetEntities<SystemItem>();
            var current_views = client.GetEntities<ViewItem>();
            client.Close();

            var iamclient = new Connection(Settings.IAMConnectionDBMS, Settings.IAMConnection);
            iamclient.Open();
            var current_permissions =  iamclient.GetEntities<IntwentyProductAuthorizationItem>();

            foreach (var t in current_systems)
            {
                if (!current_permissions.Exists(p => p.MetaCode == t.MetaCode && p.ProductId == Settings.ProductId && p.AuthorizationType == SystemModelItem.MetaTypeSystem))
                {
                    var sysauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = t.Title, NormalizedName = t.MetaCode.ToUpper(), AuthorizationType = SystemModelItem.MetaTypeSystem, ProductId = Settings.ProductId };
                    iamclient.InsertEntity(sysauth);
                }
            }


            foreach (var t in current_apps)
            {
                if (!current_permissions.Exists(p => p.MetaCode == t.MetaCode && p.ProductId == Settings.ProductId && p.AuthorizationType == ApplicationModelItem.MetaTypeApplication))
                {
                    var appauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = t.Title, NormalizedName = t.MetaCode.ToUpper(), AuthorizationType = ApplicationModelItem.MetaTypeApplication, ProductId = Settings.ProductId };
                    iamclient.InsertEntity(appauth);
                }

                foreach (var v in current_views)
                {
                    if (v.AppMetaCode == t.MetaCode)
                    {
                        if (!current_permissions.Exists(p => p.MetaCode == v.MetaCode && p.ProductId == Settings.ProductId && p.AuthorizationType == ViewModel.MetaTypeUIView))
                        {
                            var appauth = new IntwentyProductAuthorizationItem() { Id = Guid.NewGuid().ToString(), Name = t.Title + " - " + v.Title, NormalizedName = v.MetaCode.ToUpper(), AuthorizationType = ViewModel.MetaTypeUIView, ProductId = Settings.ProductId };
                            iamclient.InsertEntity(appauth);
                        }
                    }
                }
            }

            iamclient.Close();
        }

        public virtual void SeedUsersAndRoles()
        {


            var t = Task.Run(async () =>
            {
                //ENSURE WE HAVE A PRODUCT AND ORG
                IntwentyProduct product = await ProductManager.FindByIdAsync(Settings.ProductId);
                if (product == null)
                {
                    product = new IntwentyProduct();
                    product.Id = Settings.ProductId;
                    product.ProductName = Settings.ProductTitle;
                    await ProductManager.CreateAsync(product);
                }

                IntwentyOrganization org = await OrganizationManager.FindByNameAsync(Settings.ProductOrganization);
                if (org == null)
                {
                    org = new IntwentyOrganization();
                    org.Name = Settings.ProductOrganization;
                    await OrganizationManager.CreateAsync(org);
                }

                var all_products = await OrganizationManager.GetProductsAsync(org.Id);
                var thisproduct = all_products.Find(p => p.ProductId == product.Id);
                if (thisproduct == null)
                {
                    await OrganizationManager.AddProductAsync(new IntwentyOrganizationProduct() { OrganizationId = org.Id, ProductId = product.Id, ProductName = product.ProductName });
                }



                var admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleSuperAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleSuperAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleUserAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleUserAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                admrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleSystemAdmin);
                if (admrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleSystemAdmin;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                var userrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleUser);
                if (userrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleUser;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                userrole = await RoleManager.FindByNameAsync(IntwentyRoles.RoleAPIUser);
                if (userrole == null)
                {
                    var role = new IntwentyProductAuthorizationItem();
                    role.ProductId = product.Id;
                    role.Name = IntwentyRoles.RoleAPIUser;
                    role.AuthorizationType = "ROLE";
                    await RoleManager.CreateAsync(role);
                }

                if (!string.IsNullOrEmpty(Settings.DemoAdminUser) && !string.IsNullOrEmpty(Settings.DemoAdminPassword))
                {
                    IntwentyUser admin_user = await UserManager.FindByNameAsync(Settings.DemoAdminUser);
                    if (admin_user == null)
                    {
                        admin_user = new IntwentyUser();
                        admin_user.UserName = Settings.DemoAdminUser;
                        admin_user.Email = Settings.DemoAdminUser;
                        admin_user.FirstName = "Admin";
                        admin_user.LastName = "Adminsson";
                        admin_user.EmailConfirmed = true;
                        admin_user.Culture = Settings.LocalizationDefaultCulture;
                        await UserManager.CreateAsync(admin_user, Settings.DemoAdminPassword);
                    }

                    var all_members = await OrganizationManager.GetMembersAsync(org.Id);
                    var admin_member = all_members.Find(p => p.UserId == admin_user.Id);
                    if (admin_member == null)
                    {
                        await OrganizationManager.AddMemberAsync(new IntwentyOrganizationMember() { OrganizationId = org.Id, UserId = admin_user.Id, UserName = admin_user.UserName });
                        await UserManager.AddUpdateUserRoleAuthorizationAsync(IntwentyRoles.RoleSuperAdmin, admin_user.Id, org.Id, Settings.ProductId);
                    }
                }

                if (!string.IsNullOrEmpty(Settings.DemoUser) && !string.IsNullOrEmpty(Settings.DemoUserPassword))
                {
                    IntwentyUser default_user = await UserManager.FindByNameAsync(Settings.DemoUser);
                    if (default_user == null)
                    {
                        default_user = new IntwentyUser();
                        default_user.UserName = Settings.DemoUser;
                        default_user.Email = Settings.DemoUser;
                        default_user.FirstName = "User";
                        default_user.LastName = "Usersson";
                        default_user.EmailConfirmed = true;
                        default_user.Culture = Settings.LocalizationDefaultCulture;
                        await UserManager.CreateAsync(default_user, Settings.DemoUserPassword);
                    }

                    var all_members = await OrganizationManager.GetMembersAsync(org.Id);
                    var user_member = all_members.Find(p => p.UserId == default_user.Id);
                    if (user_member == null)
                    {
                        await OrganizationManager.AddMemberAsync(new IntwentyOrganizationMember() { OrganizationId = org.Id, UserId = default_user.Id, UserName = default_user.UserName });
                        await UserManager.AddUpdateUserRoleAuthorizationAsync(IntwentyRoles.RoleUser, default_user.Id, org.Id, Settings.ProductId);
                    }
                }
            });

            t.GetAwaiter().GetResult();
        }


     

        public virtual void SeedModel()
        {

        }

      

        public virtual void SeedData()
        {

        }

        public virtual void ConfigureDataBase()
        {

            if (!Settings.StartUpConfigureDatabase)
                return;

            var t = ModelService.ConfigureDatabase();

            t.GetAwaiter().GetResult();
        }

      

       
       

       

      
    }
}

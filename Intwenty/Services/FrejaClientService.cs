using Intwenty.Interface;
using Intwenty.Model;
using Intwenty.Model.FrejaEId;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Intwenty.Services
{
    public class FrejaClientService : IFrejaClientService
    {
   
        private readonly HttpClient client;
        private readonly IntwentySettings settings;

        public FrejaClientService(HttpClient http_client, IOptions<IntwentySettings> options)
        {
            this.client = http_client;
            this.settings = options.Value;
            this.client.BaseAddress = new Uri(this.settings.FrejaBaseAddress);
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public async Task<FrejaStatusResponse> InitAuthentication()
        {

            try
            {
                // Create a request
                FrejaRequest request = new FrejaRequest
                {
                    userInfoType = "INFERRED",
                    userInfo = "N/A",
                    minRegistrationLevel = settings.FrejaMinRegistrationLevel, // BASIC, EXTENDED or PLUS
                    attributesToReturn=new List<AttributesToReturnItem>()
                };

                if (!string.IsNullOrEmpty(settings.FrejaRequestedAttributes))
                {
                    var list = settings.FrejaRequestedAttributes.Split(",",StringSplitOptions.RemoveEmptyEntries);
                    foreach (var t in list)
                    {
                        request.attributesToReturn.Add(new AttributesToReturnItem() { attribute=t });
                    }
                }


                // Set serializer options
                var json_options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(request, json_options);
                var content = new StringContent("initAuthRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                content.Headers.ContentType.MediaType = "application/json";
                content.Headers.ContentType.CharSet = "utf-8";

                HttpResponseMessage response = await client.PostAsync("/authentication/1.0/initAuthentication", content);
                if (response.IsSuccessStatusCode == true)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var status_response = JsonSerializer.Deserialize<FrejaStatusResponse>(data);
                    return status_response;

                }
            }
            catch
            {
                return null;
            }


            return null;
        }

        public Uri GetQRCode(string authref)
        {

            try
            {
                var encoded_authref = WebUtility.UrlEncode(authref);
                var scheme = string.Format("frejaeid://bindUserToTransaction?transactionReference={0}", encoded_authref);
                var encoded_schema = WebUtility.UrlEncode(scheme);
                return new Uri(string.Format(settings.FrejaQRCodeEndpoint, encoded_schema));
            }
            catch
            {
            
            }
          

            return null;
        }

        public async Task<RequestedAttributes> Authenticate(string authref)
        {
         
            try
            {
                if (string.IsNullOrEmpty(authref))
                    return null;

                var json = "{" + string.Format("\"authRef\":\"{0}\"", authref) +"}";
                var content = new StringContent("getOneAuthResultRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                content.Headers.ContentType.MediaType = "application/json";
                content.Headers.ContentType.CharSet = "utf-8";
                Int32 timeout = this.settings.FrejaTimeoutInMilliseconds;

                while (true)
                {
                    // Check for a timeout
                    if (timeout <= 0)
                    {
                        // Cancel
                        content = new StringContent("cancelAuthRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                        content.Headers.ContentType.MediaType = "application/json";
                        content.Headers.ContentType.CharSet = "utf-8";
                        var cancelresponse = await client.PostAsync("/authentication/1.0/cancel", content);
                        return null;
                    }
          
                    await Task.Delay(2000);
                    timeout -= 2000;


                    var getoneresponse = await client.PostAsync("/authentication/1.0/getOneResult", content);
                    if (getoneresponse.IsSuccessStatusCode == true)
                    {
     
                        string data = await getoneresponse.Content.ReadAsStringAsync();
                        var status_response = JsonSerializer.Deserialize<FrejaStatusResponse>(data);
                        if (status_response.HasApprovedStatus)
                        {
                            return status_response.requestedAttributes;
                        }
                        else if (status_response.HasWaitForUserStatus)
                        {
                            continue;
                        }
                        else
                        {
                            // CANCELED, RP_CANCELED, EXPIRED or REJECTED
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
              
            }
            catch
            {
                
            }
           
            return null;
        }


        public async Task<RequestedAttributes> Authenticate(string userInfoType, string userInfo)
        {

            StringContent content = null;
            FrejaStatusResponse status_response = null;

            try
            {
                
                FrejaRequest request = new FrejaRequest
                {
                    userInfoType = userInfoType,
                    userInfo = userInfo,
                    minRegistrationLevel = "PLUS", // BASIC, EXTENDED or PLUS
                    attributesToReturn = new List<AttributesToReturnItem>()
                };

                if (!string.IsNullOrEmpty(settings.FrejaRequestedAttributes))
                {
                    var list = settings.FrejaRequestedAttributes.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var t in list)
                    {
                        request.attributesToReturn.Add(new AttributesToReturnItem() { attribute = t });
                    }
                }


                var json_options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

       
                string json = JsonSerializer.Serialize(request, json_options);
           
                content = new StringContent("initAuthRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                content.Headers.ContentType.MediaType = "application/json";
                content.Headers.ContentType.CharSet = "utf-8";

                HttpResponseMessage response = await client.PostAsync("/authentication/1.0/initAuthentication", content);
                if (response.IsSuccessStatusCode == true)
                {
            
                    json = await response.Content.ReadAsStringAsync();
                    content = new StringContent("getOneAuthResultRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                    content.Headers.ContentType.MediaType = "application/json";
                    content.Headers.ContentType.CharSet = "utf-8";
                    Int32 timeout = this.settings.FrejaTimeoutInMilliseconds;
                    while (true)
                    {
                        // Check for a timeout
                        if (timeout <= 0)
                        {
                            // Cancel the order and return false
                            content = new StringContent("cancelAuthRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                            content.Headers.ContentType.MediaType = "application/json";
                            content.Headers.ContentType.CharSet = "utf-8";
                            response = await client.PostAsync("/authentication/1.0/cancel", content);
                            return null;
                        }
       
                        await Task.Delay(2000);
                        timeout -= 2000;
             
                        response = await client.PostAsync("/authentication/1.0/getOneResult", content);
                        if (response.IsSuccessStatusCode == true)
                        {
                            string data = await response.Content.ReadAsStringAsync();
                            status_response = JsonSerializer.Deserialize<FrejaStatusResponse>(data);
                            if (status_response.HasApprovedStatus)
                            {
                                return status_response.requestedAttributes;
                            }
                            else if (status_response.HasWaitForUserStatus)
                            {
                                continue;
                            }
                            else
                            {
                                // CANCELED, RP_CANCELED, EXPIRED or REJECTED
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                
            }
            finally
            {
                if (content != null)
                {
                    content.Dispose();
                }
            }

            return null;
        }

        /*

     public async Task<bool> Sign(string userInfoType, string userInfo, Signature signature)
     {


         // Variables
         StringContent content = null;
         FrejaStatusResponse status_response = null;
         try
         {
             // Create a request
             FrejaRequest request = new FrejaRequest
             {
                 userInfoType = userInfoType,
                 userInfo = userInfo,
                 minRegistrationLevel = "BASIC", // BASIC, EXTENDED or PLUS
                 title = "Sign File",
                 pushNotification = new PushNotification // Can not include swedish characters å,ä,ö
                 {
                     title = "Hello - Hallå",
                     text = "Please sign this file - Signera denna fil"
                 },
                 expiry = (Int64)DateTime.UtcNow.AddMinutes(5).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds,
                 //expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds(),
                 dataToSignType = "SIMPLE_UTF8_TEXT",
                 dataToSign = new DataToSign { text = Convert.ToBase64String(Encoding.UTF8.GetBytes(signature.data)) },
                 signatureType = "SIMPLE",
                 attributesToReturn = new List<AttributesToReturnItem>
                 {
                     //new AttributesToReturnItem
                     //{
                     //    attribute = "BASIC_USER_INFO",
                     //},
                     new AttributesToReturnItem
                     {
                         attribute = "EMAIL_ADDRESS",
                     },
                     //new AttributesToReturnItem
                     //{
                     //    attribute = "DATE_OF_BIRTH",
                     //},
                     //new AttributesToReturnItem
                     //{
                     //    attribute = "ADDRESSES",
                     //},
                     //new AttributesToReturnItem
                     //{
                     //    attribute = "SSN",
                     //}
                 }
             };

             var json_options = new JsonSerializerOptions
             {
                 IgnoreNullValues = true,
                 WriteIndented = true,
                 Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
             };

             // Convert request to json
             string json = JsonSerializer.Serialize(request, json_options);

             // Create string content
             content = new StringContent("initSignRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
             content.Headers.ContentType.MediaType = "application/json";
             content.Headers.ContentType.CharSet = "utf-8";

             // Get the response
             HttpResponseMessage response = await client.PostAsync("/sign/1.0/initSignature", content);

             if (response.IsSuccessStatusCode == true)
             {
                 // Get string data
                 json = await response.Content.ReadAsStringAsync();

                 // Add content
                 content = new StringContent("getOneSignResultRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                 content.Headers.ContentType.MediaType = "application/json";
                 content.Headers.ContentType.CharSet = "utf-8";

                 // Collect the signature
                 Int32 timeout = this.settings.FrejaTimeoutInMilliseconds;
                 while (true)
                 {
                     // Check for a timeout
                     if (timeout <= 0)
                     {
                         // Cancel the order and return false
                         content = new StringContent("cancelSignRequest=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
                         content.Headers.ContentType.MediaType = "application/json";
                         content.Headers.ContentType.CharSet = "utf-8";
                         response = await client.PostAsync("/sign/1.0/cancel", content);
                         return false;
                     }

                     // Sleep for 2 seconds
                     await Task.Delay(2000);

                     timeout -= 2000;

                     // Collect a signature
                     response = await client.PostAsync("/sign/1.0/getOneResult", content);
                     if (response.IsSuccessStatusCode == true)
                     {
                         // Get string data
                         string data = await response.Content.ReadAsStringAsync();

                         // Convert data to a bankid response
                         status_response = JsonSerializer.Deserialize<FrejaStatusResponse>(data);

                         if (status_response.status == "APPROVED")
                         {
                             // Break out from the loop
                             break;

                         }
                         else if (status_response.status == "STARTED" || status_response.status == "DELIVERED_TO_MOBILE" || status_response.status == "OPENED" || status_response.status == "OPENED")
                         {
                             continue;
                         }
                         else
                         {
                             // CANCELED, RP_CANCELED or EXPIRED
                             return false;
                         }
                     }
                     else
                     {

                         string data = await response.Content.ReadAsStringAsync();
                         return false;
                     }
                 }
             }
             else
             {

                 string data = await response.Content.ReadAsStringAsync();
                 return false;
             }

             // Update the signature
             signature.algorithm = "SHA-256";
             signature.padding = "Pkcs1";
             signature.value = status_response.details;
             signature.certificate = this.settings.FrejaJWSCertificate;
         }
         catch (Exception ex)
         {
             return false;
         }
         finally
         {
             if (content != null)
             {
                 content.Dispose();
             }
         }

         return true;

     } 
            */

        /*
        public SignatureValidationResult Validate(Signature signature)
        {


            // Create the result to return
            SignatureValidationResult result = new SignatureValidationResult();
            result.signature_data = signature.data;


            // Get JWS data (signed by Freja)
            string[] jws = signature.value.Split('.');
            byte[] data = Encoding.UTF8.GetBytes(jws[0] + "." + jws[1]);
            byte[] digest = WebEncoders.Base64UrlDecode(jws[2]);


            // Get payload data
            FrejaPayload payload = JsonSerializer.Deserialize<FrejaPayload>(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(jws[1])));
            result.signatory = payload.userInfoType + ": " + payload.userInfo;
            string[] user_signature = payload.signatureData.userSignature.Split('.');
            string signed_data = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(user_signature[1]));
            try
            {
                // Get the certificate
                result.certificate = new X509Certificate2(Convert.FromBase64String(signature.certificate));
                // Get the public key
                using (RSA rsa = result.certificate.GetRSAPublicKey())
                {
                    // Check if the signature is valid
                    result.valid = rsa.VerifyData(data, digest, GetHashAlgorithmName(signature.algorithm), GetRSASignaturePadding(signature.padding));
                }
            }
            catch (Exception ex)
            {

            }

            // Make sure that signature data conforms
            if (signature.data != signed_data)
            {
                result.valid = false;
            }


            return result;

        } 
        */


        public static HashAlgorithmName GetHashAlgorithmName(string signature_algorithm)
        {
            if (signature_algorithm == "SHA-256")
            {
                return HashAlgorithmName.SHA256;
            }
            else if (signature_algorithm == "SHA-384")
            {
                return HashAlgorithmName.SHA384;
            }
            else if (signature_algorithm == "SHA-512")
            {
                return HashAlgorithmName.SHA512;
            }
            else
            {
                return HashAlgorithmName.SHA1;
            }
        } 

        public static RSASignaturePadding GetRSASignaturePadding(string signature_padding)
        {
            if (signature_padding == "Pss")
            {
                return RSASignaturePadding.Pss;
            }
            else
            {
                return RSASignaturePadding.Pkcs1;
            }
        } 
   
    } 
}

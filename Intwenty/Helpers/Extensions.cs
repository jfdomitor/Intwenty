using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Intwenty.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Encodes a string to url safe Base64, except if it begins with UENC
        /// </summary>
        public static string B64UrlEncode(this string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                    return string.Empty;

                if (s.StartsWith("UENC"))
                    return s;

                return Base64UrlEncoder.Encode(s);

            }
            catch { }

            return string.Empty;
        }

        /// <summary>
        /// Decodes an url safe Base64 to string, except if it begins with UENC
        /// </summary>
        public static string B64UrlDecode(this string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                    return string.Empty;

                if (s.StartsWith("UENC"))
                    return s;

                return Base64UrlEncoder.Decode(s);

            }
            catch { }

            return string.Empty;
        }

    }
}

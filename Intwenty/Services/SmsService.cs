using Intwenty.Interface;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intwenty.Model;

namespace IntwentyDemo.Services
{
    
    public class SmsService : IIntwentySmsService
    {

        public IntwentySettings Settings { get; }

        public SmsService(IOptions<IntwentySettings> settings)
        {
            Settings = settings.Value;
        }


        public async Task<bool> SendSmsAsync(string number, string message)
        {
            return await Task.FromResult(true);
        }
    }

}
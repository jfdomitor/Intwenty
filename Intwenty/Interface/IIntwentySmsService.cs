using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Interface
{
    public interface IIntwentySmsService
    {
        Task<bool> SendSmsAsync(string number, string message);
    }

}

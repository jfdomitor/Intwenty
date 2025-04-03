using Intwenty.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Intwenty.Interface
{
    public interface IIntwentyEventService
    {
        Task NewUserCreated(NewUserCreatedData data);
        Task EmailChanged(EmailChangedData data);
        Task UserActivatedEmailMfa(UserActivatedEmailMfaData data);
        Task UserActivatedSmsMfa(UserActivatedSmsMfaData data);
        Task UserRequestedEmailMfaCode(UserRequestedEmailMfaCodeData data);
        Task UserRequestedSmsMfaCode(UserRequestedSmsMfaCodeData data);
        Task UserRequestedPasswordReset(UserRequestedPasswordResetData data);
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner.Component
{
    public enum SignInResult
    {
        Success = -1,
        Other = 0,
        InvalidCredential,
        DuplicateSignIn,
    }
}
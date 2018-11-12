using System;
using System.Runtime.InteropServices;
using System.Security;

namespace HeroBot
{
    public static class SecureStringExtenstions
    {
        public static SecureString ToSecureString(this string str)
        {
            SecureString secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }

            return secureString;
        }

        public static string ToUnsecureString(this SecureString secureString)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}

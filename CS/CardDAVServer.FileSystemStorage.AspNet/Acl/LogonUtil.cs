using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Contains helper Win32 methods.
    /// </summary>
    public static class LogonUtil
    {
        /// <summary>
        /// Retrieves user by username and password.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="password">Password.</param>
        /// <exception cref="Exception">If user cannot be authenticated.</exception>
        /// <returns>Authenticated user.</returns>
        public static WindowsIdentity GetUser(string username, string domain, string password)
        {
            SafeTokenHandle existingTokenHandle = SafeTokenHandle.InvalidHandle;

            if (string.IsNullOrEmpty(domain))
            {
                domain = Environment.MachineName;
            }

            try
            {
                const int LOGON32_PROVIDER_DEFAULT = 0;
                const int LOGON32_LOGON_INTERACTIVE = 2;

                bool impersonated = LogonUser(
                    username,
                    domain,
                    password,
                    LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT,
                    out existingTokenHandle);

                if (false == impersonated)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    string result = "LogonUser() failed with error code: " + errorCode + Environment.NewLine;
                    throw new Exception(result);
                }
            
                return new WindowsIdentity(existingTokenHandle.DangerousGetHandle());
            }
            finally
            {
                if (!existingTokenHandle.IsInvalid)
                {
                    existingTokenHandle.Close();
                }
            }
        }

        public static WindowsIdentity DuplicateToken(WindowsIdentity id)
        {
            SafeTokenHandle duplicateTokenHandle = SafeTokenHandle.InvalidHandle;
            try
            {
                bool bRetVal = DuplicateToken(id.Token, 2, out duplicateTokenHandle);

                if (false == bRetVal)
                {
                    int nErrorCode = Marshal.GetLastWin32Error();
                    string sResult = "DuplicateToken() failed with error code: " + nErrorCode + Environment.NewLine;
                    throw new Exception(sResult);
                }

                return new WindowsIdentity(duplicateTokenHandle.DangerousGetHandle());
            }
            finally
            {
                if (!duplicateTokenHandle.IsInvalid)
                {
                    duplicateTokenHandle.Close();
                }
            }
        }

        private sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle()
                : base(true)
            {
            }

            internal SafeTokenHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
             DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }

            internal static SafeTokenHandle InvalidHandle
            {
                get { return new SafeTokenHandle(IntPtr.Zero); }
            }
        }

        [DllImport("ADVAPI32.DLL")]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out SafeTokenHandle phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateToken(
            IntPtr existingTokenHandle,
            int securityImpersonationLevel,
            out SafeTokenHandle duplicateTokenHandle);
    }
}

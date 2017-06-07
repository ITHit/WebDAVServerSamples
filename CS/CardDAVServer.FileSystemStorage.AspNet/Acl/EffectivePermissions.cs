using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Helper class to retrieve effective permissions for file or folder.
    /// </summary>
    internal class EffectivePermissions
    {
        internal enum SecurityImpersonationLevel
        {
            Anonymous,
            Identification,
            Impersonation,
            Delegation
        }

        internal enum TokenType
        {
            TokenImpersonation = 2,
            TokenPrimary = 1
        }

        internal enum TOKEN_INFORMATION_CLASS
        {
            TokenDefaultDacl = 6,
            TokenGroups = 2,
            TokenGroupsAndPrivileges = 13,
            TokenImpersonationLevel = 9,
            TokenOwner = 4,
            TokenPrimaryGroup = 5,
            TokenPrivileges = 3,
            TokenRestrictedSids = 11,
            TokenSandBoxInert = 15,
            TokenSessionId = 12,
            TokenSessionReference = 14,
            TokenSource = 7,
            TokenStatistics = 10,
            TokenType = 8,
            TokenUser = 1
        }

        [Flags]
        internal enum PrivilegeAttribute : uint
        {
            SE_PRIVILEGE_DISABLED = 0,
            SE_PRIVILEGE_ENABLED = 2,
            SE_PRIVILEGE_ENABLED_BY_DEFAULT = 1,
            SE_PRIVILEGE_REMOVED = 4,
            SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000
        }

        public static FileSystemRights GetEffectivePermissions(
            WindowsIdentity clientIdentity,
            FileSecurity securityDescriptor)
        {
            bool isAccessAllowed = false;
            byte[] binaryForm = securityDescriptor.GetSecurityDescriptorBinaryForm();
            SafeCloseHandle newToken = null;
            SafeCloseHandle token = new SafeCloseHandle(clientIdentity.Token, false);
            try
            {
                if (IsPrimaryToken(token) &&
                    !DuplicateTokenEx(
                        token,
                        TokenAccessLevels.Query,
                        IntPtr.Zero,
                        SecurityImpersonationLevel.Identification,
                        TokenType.TokenImpersonation,
                        out newToken))
                {
                    int err = Marshal.GetLastWin32Error();
                    CloseInvalidOutSafeHandle(newToken);
                    throw new Win32Exception(err, "DuplicateTokenExFailed");
                }

                GENERIC_MAPPING genericMapping = new GENERIC_MAPPING();
                PRIVILEGE_SET structPrivilegeSet = new PRIVILEGE_SET();
                uint privilegeSetLength = (uint)Marshal.SizeOf(structPrivilegeSet);
                uint grantedAccess = 0;
                if (!AccessCheck(
                    binaryForm,
                    newToken ?? token,
                    0x2000000,
                    genericMapping,
                    out structPrivilegeSet,
                    ref privilegeSetLength,
                    out grantedAccess,
                    out isAccessAllowed))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "AccessCheckFailed");
                }

                return (FileSystemRights)grantedAccess;
            }
            finally
            {
                if (newToken != null)
                {
                    newToken.Dispose();
                }
            }
        }

        public static SafeHandle GetTokenInformation(SafeCloseHandle token, TOKEN_INFORMATION_CLASS infoClass)
        {
            uint num;
            if (!GetTokenInformation(token, infoClass, SafeHGlobalHandle.InvalidHandle, 0, out num))
            {
                int err = Marshal.GetLastWin32Error();
                if (err != 0x7a)
                {
                    throw new Win32Exception(err, "GetTokenInfoFailed");
                }
            }

            SafeHandle tokenInformation = SafeHGlobalHandle.AllocHGlobal(num);
            try
            {
                if (!GetTokenInformation(token, infoClass, tokenInformation, num, out num))
                {
                    int num3 = Marshal.GetLastWin32Error();
                    throw new Win32Exception(num3, "GetTokenInfoFailed");
                }
            }
            catch
            {
                tokenInformation.Dispose();
                throw;
            }

            return tokenInformation;
        }

        internal static bool IsPrimaryToken(SafeCloseHandle token)
        {
            using (SafeHandle handle = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenType))
            {
                return Marshal.ReadInt32(handle.DangerousGetHandle()) == 1;
            }
        }

        internal static void CloseInvalidOutSafeHandle(SafeHandle handle)
        {
            if (handle != null)
            {
                handle.SetHandleAsInvalid();
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool AccessCheck(
            [In] byte[] securityDescriptor,
            [In] SafeCloseHandle clientToken,
            [In] int desiredAccess,
            [In] GENERIC_MAPPING genericMapping,
            out PRIVILEGE_SET privilegeSet,
            [In, Out] ref uint privilegeSetLength,
            out uint grantedAccess,
            out bool accessStatus);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool DuplicateTokenEx(
            [In] SafeCloseHandle existingToken,
            [In] TokenAccessLevels desiredAccess,
            [In] IntPtr tokenAttributes,
            [In] SecurityImpersonationLevel impersonationLevel,
            [In] TokenType tokenType,
            out SafeCloseHandle newToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool GetTokenInformation(
            [In] SafeCloseHandle tokenHandle,
            [In] TOKEN_INFORMATION_CLASS tokenInformationClass,
            [In] SafeHandle tokenInformation,
            [Out] uint tokenInformationLength,
            out uint returnLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal PrivilegeAttribute Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            internal uint LowPart;
            internal int HighPart;
        }

        internal sealed class SafeCloseHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeCloseHandle(IntPtr handle, bool ownsHandle)
                : base(ownsHandle)
            {
                this.SetHandle(handle);
            }

            private SafeCloseHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(this.handle);
            }

            [SuppressUnmanagedCodeSecurity,
             ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
             DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            private static extern bool CloseHandle(IntPtr handle);
        }

        internal sealed class SafeHGlobalHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            private SafeHGlobalHandle()
                : base(true)
            {
            }

            private SafeHGlobalHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            // Properties
            public static SafeHGlobalHandle InvalidHandle
            {
                get
                {
                    return new SafeHGlobalHandle(IntPtr.Zero);
                }
            }

            public static SafeHGlobalHandle AllocHGlobal(int cb)
            {
                if (cb < 0)
                {
                    throw new ArgumentOutOfRangeException("cb", "ValueMustBeNonNegative");
                }

                SafeHGlobalHandle handle = new SafeHGlobalHandle();
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    IntPtr ptr = Marshal.AllocHGlobal(cb);
                    handle.SetHandle(ptr);
                }

                return handle;
            }

            public static SafeHGlobalHandle AllocHGlobal(byte[] bytes)
            {
                SafeHGlobalHandle handle = AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, handle.DangerousGetHandle(), bytes.Length);
                return handle;
            }

            public static SafeHGlobalHandle AllocHGlobal(string s)
            {
                byte[] bytes = new byte[((s.Length + 1) * 2)];
                Encoding.Unicode.GetBytes(s, 0, s.Length, bytes, 0);
                return AllocHGlobal(bytes);
            }

            public static SafeHGlobalHandle AllocHGlobal(uint cb)
            {
                return AllocHGlobal((int)cb);
            }

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PRIVILEGE_SET
        {
            internal uint PrivilegeCount = 1;
            internal uint Control;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class GENERIC_MAPPING
        {
            internal uint GenericRead;
            internal uint GenericWrite;
            internal uint GenericExecute;
            internal uint GenericAll;
        }
    }
}

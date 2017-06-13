Imports System
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.ConstrainedExecution
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Text
Imports Microsoft.Win32.SafeHandles

Namespace Acl

    ''' <summary>
    ''' Helper class to retrieve effective permissions for file or folder.
    ''' </summary>
    Friend Class EffectivePermissions

        Friend Enum SecurityImpersonationLevel
            Anonymous
            Identification
            Impersonation
            Delegation
        End Enum

        Friend Enum TokenType
            TokenImpersonation = 2
            TokenPrimary = 1
        End Enum

        Friend Enum TOKEN_INFORMATION_CLASS
            TokenDefaultDacl = 6
            TokenGroups = 2
            TokenGroupsAndPrivileges = 13
            TokenImpersonationLevel = 9
            TokenOwner = 4
            TokenPrimaryGroup = 5
            TokenPrivileges = 3
            TokenRestrictedSids = 11
            TokenSandBoxInert = 15
            TokenSessionId = 12
            TokenSessionReference = 14
            TokenSource = 7
            TokenStatistics = 10
            TokenType = 8
            TokenUser = 1
        End Enum

        <Flags>
        Friend Enum PrivilegeAttribute As UInteger
            SE_PRIVILEGE_DISABLED = 0
            SE_PRIVILEGE_ENABLED = 2
            SE_PRIVILEGE_ENABLED_BY_DEFAULT = 1
            SE_PRIVILEGE_REMOVED = 4
            SE_PRIVILEGE_USED_FOR_ACCESS = 2147483648UI
        End Enum

        Public Shared Function GetEffectivePermissions(clientIdentity As WindowsIdentity,
                                                      securityDescriptor As FileSecurity) As FileSystemRights
            Dim isAccessAllowed As Boolean = False
            Dim binaryForm As Byte() = securityDescriptor.GetSecurityDescriptorBinaryForm()
            Dim newToken As SafeCloseHandle = Nothing
            Dim token As SafeCloseHandle = New SafeCloseHandle(clientIdentity.Token, False)
            Try
                If IsPrimaryToken(token) AndAlso Not DuplicateTokenEx(token,
                                                                     TokenAccessLevels.Query,
                                                                     IntPtr.Zero,
                                                                     SecurityImpersonationLevel.Identification,
                                                                     TokenType.TokenImpersonation,
                                                                     newToken) Then
                    Dim err As Integer = Marshal.GetLastWin32Error()
                    CloseInvalidOutSafeHandle(newToken)
                    Throw New Win32Exception(err, "DuplicateTokenExFailed")
                End If

                Dim genericMapping As GENERIC_MAPPING = New GENERIC_MAPPING()
                Dim structPrivilegeSet As PRIVILEGE_SET = New PRIVILEGE_SET()
                Dim privilegeSetLength As UInteger = CUInt(Marshal.SizeOf(structPrivilegeSet))
                Dim grantedAccess As UInteger = 0
                If Not AccessCheck(binaryForm,
                                  If(newToken, token),
                                  33554432,
                                  genericMapping,
                                  structPrivilegeSet,
                                  privilegeSetLength,
                                  grantedAccess,
                                  isAccessAllowed) Then
                    Throw New Win32Exception(Marshal.GetLastWin32Error(), "AccessCheckFailed")
                End If

                Return CType(grantedAccess, FileSystemRights)
            Finally
                If newToken IsNot Nothing Then
                    newToken.Dispose()
                End If
            End Try
        End Function

        Public Shared Function GetTokenInformation(token As SafeCloseHandle, infoClass As TOKEN_INFORMATION_CLASS) As SafeHandle
            Dim num As UInteger
            If Not GetTokenInformation(token, infoClass, SafeHGlobalHandle.InvalidHandle, 0, num) Then
                Dim err As Integer = Marshal.GetLastWin32Error()
                If err <> 122 Then
                    Throw New Win32Exception(err, "GetTokenInfoFailed")
                End If
            End If

            Dim tokenInformation As SafeHandle = SafeHGlobalHandle.AllocHGlobal(num)
            Try
                If Not GetTokenInformation(token, infoClass, tokenInformation, num, num) Then
                    Dim num3 As Integer = Marshal.GetLastWin32Error()
                    Throw New Win32Exception(num3, "GetTokenInfoFailed")
                End If
            Catch
                tokenInformation.Dispose()
                Throw
            End Try

            Return tokenInformation
        End Function

        Friend Shared Function IsPrimaryToken(token As SafeCloseHandle) As Boolean
            Using handle As SafeHandle = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenType)
                Return Marshal.ReadInt32(handle.DangerousGetHandle()) = 1
            End Using
        End Function

        Friend Shared Sub CloseInvalidOutSafeHandle(handle As SafeHandle)
            If handle IsNot Nothing Then
                handle.SetHandleAsInvalid()
            End If
        End Sub

        <DllImport("advapi32.dll", SetLastError:=True)>
        Friend Shared Function AccessCheck(<[In]> securityDescriptor As Byte(),
                                          <[In]> clientToken As SafeCloseHandle,
                                          <[In]> desiredAccess As Integer,
                                          <[In]> genericMapping As GENERIC_MAPPING,
                                          ByRef privilegeSet As PRIVILEGE_SET,
                                          <[In], Out> ByRef privilegeSetLength As UInteger,
                                          ByRef grantedAccess As UInteger,
                                          ByRef accessStatus As Boolean) As Boolean
        End Function

        <DllImport("advapi32.dll", SetLastError:=True)>
        Friend Shared Function DuplicateTokenEx(<[In]> existingToken As SafeCloseHandle,
                                               <[In]> desiredAccess As TokenAccessLevels,
                                               <[In]> tokenAttributes As IntPtr,
                                               <[In]> impersonationLevel As SecurityImpersonationLevel,
                                               <[In]> tokenType As TokenType,
                                               ByRef newToken As SafeCloseHandle) As Boolean
        End Function

        <DllImport("advapi32.dll", SetLastError:=True)>
        Friend Shared Function GetTokenInformation(<[In]> tokenHandle As SafeCloseHandle,
                                                  <[In]> tokenInformationClass As TOKEN_INFORMATION_CLASS,
                                                  <[In]> tokenInformation As SafeHandle,
                                                  <Out> tokenInformationLength As UInteger,
                                                  ByRef returnLength As UInteger) As Boolean
        End Function

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure LUID_AND_ATTRIBUTES

            Friend Luid As LUID

            Friend Attributes As PrivilegeAttribute
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure LUID

            Friend LowPart As UInteger

            Friend HighPart As Integer
        End Structure

        Friend NotInheritable Class SafeCloseHandle
            Inherits SafeHandleZeroOrMinusOneIsInvalid

            Friend Sub New(handle As IntPtr, ownsHandle As Boolean)
                MyBase.New(ownsHandle)
                Me.SetHandle(handle)
            End Sub

            Private Sub New()
                MyBase.New(True)
            End Sub

            Protected Overrides Function ReleaseHandle() As Boolean
                Return CloseHandle(Me.handle)
            End Function

            <SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", SetLastError:=True, ExactSpelling:=True)>
            Private Shared Function CloseHandle(handle As IntPtr) As Boolean
            End Function
        End Class

        Friend NotInheritable Class SafeHGlobalHandle
            Inherits SafeHandleZeroOrMinusOneIsInvalid

            ' Methods
            Private Sub New()
                MyBase.New(True)
            End Sub

            Private Sub New(handle As IntPtr)
                MyBase.New(True)
                SetHandle(handle)
            End Sub

            ' Properties
            Public Shared ReadOnly Property InvalidHandle As SafeHGlobalHandle
                Get
                    Return New SafeHGlobalHandle(IntPtr.Zero)
                End Get
            End Property

            Public Shared Function AllocHGlobal(cb As Integer) As SafeHGlobalHandle
                If cb < 0 Then
                    Throw New ArgumentOutOfRangeException("cb", "ValueMustBeNonNegative")
                End If

                Dim handle As SafeHGlobalHandle = New SafeHGlobalHandle()
                RuntimeHelpers.PrepareConstrainedRegions()
                Try
                     Finally
                    Dim ptr As IntPtr = Marshal.AllocHGlobal(cb)
                    handle.SetHandle(ptr)
                End Try

                Return handle
            End Function

            Public Shared Function AllocHGlobal(bytes As Byte()) As SafeHGlobalHandle
                Dim handle As SafeHGlobalHandle = AllocHGlobal(bytes.Length)
                Marshal.Copy(bytes, 0, handle.DangerousGetHandle(), bytes.Length)
                Return handle
            End Function

            Public Shared Function AllocHGlobal(s As String) As SafeHGlobalHandle
                Dim bytes As Byte() = New Byte(((s.Length + 1) * 2) - 1) {}
                Encoding.Unicode.GetBytes(s, 0, s.Length, bytes, 0)
                Return AllocHGlobal(bytes)
            End Function

            Public Shared Function AllocHGlobal(cb As UInteger) As SafeHGlobalHandle
                Return AllocHGlobal(CInt(cb))
            End Function

            Protected Overrides Function ReleaseHandle() As Boolean
                Marshal.FreeHGlobal(handle)
                Return True
            End Function
        End Class

        <StructLayout(LayoutKind.Sequential)>
        Friend Class PRIVILEGE_SET

            Friend PrivilegeCount As UInteger = 1

            Friend Control As UInteger

            Friend Privilege As LUID_AND_ATTRIBUTES
        End Class

        <StructLayout(LayoutKind.Sequential)>
        Friend Class GENERIC_MAPPING

            Friend GenericRead As UInteger

            Friend GenericWrite As UInteger

            Friend GenericExecute As UInteger

            Friend GenericAll As UInteger
        End Class
    End Class
End Namespace

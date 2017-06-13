Imports System
Imports System.Runtime.ConstrainedExecution
Imports System.Runtime.InteropServices
Imports System.Security.Principal
Imports Microsoft.Win32.SafeHandles

Namespace Acl

    ''' <summary>
    ''' Contains helper Win32 methods.
    ''' </summary>
    Module LogonUtil

        ''' <summary>
        ''' Retrieves user by username and password.
        ''' </summary>
        ''' <param name="username">User name.</param>
        ''' <param name="domain">Domain.</param>
        ''' <param name="password">Password.</param>
        ''' <exception cref="Exception">If user cannot be authenticated.</exception>
        ''' <returns>Authenticated user.</returns>
        Function GetUser(username As String, domain As String, password As String) As WindowsIdentity
            Dim existingTokenHandle As SafeTokenHandle = SafeTokenHandle.InvalidHandle
            If String.IsNullOrEmpty(domain) Then
                domain = Environment.MachineName
            End If

            Try
                Const LOGON32_PROVIDER_DEFAULT As Integer = 0
                Const LOGON32_LOGON_INTERACTIVE As Integer = 2
                Dim impersonated As Boolean = LogonUser(username,
                                                       domain,
                                                       password,
                                                       LOGON32_LOGON_INTERACTIVE,
                                                       LOGON32_PROVIDER_DEFAULT,
                                                       existingTokenHandle)
                If False = impersonated Then
                    Dim errorCode As Integer = Marshal.GetLastWin32Error()
                    Dim result As String = "LogonUser() failed with error code: " & errorCode & Environment.NewLine
                    Throw New Exception(result)
                End If

                Return New WindowsIdentity(existingTokenHandle.DangerousGetHandle())
            Finally
                If Not existingTokenHandle.IsInvalid Then
                    existingTokenHandle.Close()
                End If
            End Try
        End Function

        Function DuplicateToken(id As WindowsIdentity) As WindowsIdentity
            Dim duplicateTokenHandle As SafeTokenHandle = SafeTokenHandle.InvalidHandle
            Try
                Dim bRetVal As Boolean = DuplicateToken(id.Token, 2, duplicateTokenHandle)
                If False = bRetVal Then
                    Dim nErrorCode As Integer = Marshal.GetLastWin32Error()
                    Dim sResult As String = "DuplicateToken() failed with error code: " & nErrorCode & Environment.NewLine
                    Throw New Exception(sResult)
                End If

                Return New WindowsIdentity(duplicateTokenHandle.DangerousGetHandle())
            Finally
                If Not duplicateTokenHandle.IsInvalid Then
                    duplicateTokenHandle.Close()
                End If
            End Try
        End Function

        Private NotInheritable Class SafeTokenHandle
            Inherits SafeHandleZeroOrMinusOneIsInvalid

            Private Sub New()
                MyBase.New(True)
            End Sub

            Friend Sub New(handle As IntPtr)
                MyBase.New(True)
                SetHandle(handle)
            End Sub

            <ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", SetLastError:=True)>
            Friend Shared Function CloseHandle(handle As IntPtr) As Boolean
            End Function

            Protected Overrides Function ReleaseHandle() As Boolean
                Return CloseHandle(handle)
            End Function

            Friend Shared ReadOnly Property InvalidHandle As SafeTokenHandle
                Get
                    Return New SafeTokenHandle(IntPtr.Zero)
                End Get
            End Property
        End Class

        <DllImport("ADVAPI32.DLL")>
        Private Function LogonUser(lpszUsername As String,
                                  lpszDomain As String,
                                  lpszPassword As String,
                                  dwLogonType As Integer,
                                  dwLogonProvider As Integer,
                                  ByRef phToken As SafeTokenHandle) As Boolean
        End Function

        <DllImport("advapi32.dll", SetLastError:=True)>
        Private Function DuplicateToken(existingTokenHandle As IntPtr,
                                       securityImpersonationLevel As Integer,
                                       ByRef duplicateTokenHandle As SafeTokenHandle) As Boolean
        End Function
    End Module
End Namespace

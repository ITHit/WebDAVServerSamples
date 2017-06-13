Imports System
Imports System.Web
Imports System.Text
Imports System.Security.Principal
Imports System.Security
Imports System.Web.Security

''' <summary>
''' ASP.NET module which implements 'Basic' authentication protocol.
''' </summary>
Public Class BasicAuthenticationModule
    Inherits AuthenticationModuleBase

    ''' <summary>
    ''' Checks whether authorization header is present.
    ''' </summary>
    ''' <param name="request">Instance of <see cref="HttpRequest"/> .</param>
    ''' <returns>'true' if there's basic authentication header.</returns>
    Protected Overrides Function IsAuthorizationPresent(request As HttpRequest) As Boolean
        Dim auth As String = request.Headers("Authorization")
        Return auth IsNot Nothing AndAlso auth.Substring(0, 5).ToLower() = "basic"
    End Function

    ''' <summary>
    ''' Performs request authentication.
    ''' </summary>
    ''' <param name="request">Instance of <see cref="HttpRequest"/> .</param>
    ''' <returns>Instance of <see cref="IPrincipal"/> , or <c>null</c> if user was not authenticated.</returns>
    Protected Overrides Function AuthenticateRequest(request As HttpRequest) As IPrincipal
        Dim auth As String = request.Headers("Authorization")
        ' decode username and password
        Dim base64Credentials As String = auth.Substring(6)
        Dim bytesCredentials As Byte() = Convert.FromBase64String(base64Credentials)
        Dim credentials As String() = New UTF8Encoding().GetString(bytesCredentials).Split(":"c)
        Dim userName As String = credentials(0)
        Dim password As String = credentials(1)
        ' Windows Vista sends user name in the form DOMAIN\User
        Dim delimiterIndex As Integer = userName.IndexOf("\"c)
        If delimiterIndex <> -1 Then
            userName = userName.Remove(0, delimiterIndex + 1)
        End If

        Try
            If Membership.ValidateUser(userName, password) Then
                ' authenticated succesefully
                Return New GenericPrincipal(New GenericIdentity(userName), Nothing)
            Else
                ' invalid credentials
                Return Nothing
            End If
        Catch ex As Exception
            Logger.Instance.LogError("Failed to authenticate user", ex)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Gets challenge string.
    ''' </summary>
    ''' <returns>Challenge string.</returns>
    Protected Overrides Function GetChallenge() As String
        Return "Basic Realm=""My WebDAV Server"""
    End Function
End Class

Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Linq
Imports System.Web
Imports System.Web.Configuration
Imports System.Web.Security

''' <summary>
''' Implementation of membership provider which takes user names and passwords from standard section 
''' in web.config/app.config.
''' </summary>
''' <remarks>
''' It can be replaced with any other existing membership provider in web.config/app.config.
''' </remarks>
Public Class FormsMembershipProvider
    Inherits MembershipProvider

    Public Overrides Property ApplicationName As String
        Get
            Throw New NotImplementedException()
        End Get

        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Overrides Function ChangePassword(username As String, oldPassword As String, newPassword As String) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function ChangePasswordQuestionAndAnswer(username As String,
                                                             password As String,
                                                             newPasswordQuestion As String,
                                                             newPasswordAnswer As String) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function CreateUser(username As String,
                                        password As String,
                                        email As String,
                                        passwordQuestion As String,
                                        passwordAnswer As String,
                                        isApproved As Boolean,
                                        providerUserKey As Object,
                                        ByRef status As MembershipCreateStatus) As MembershipUser
        Throw New NotImplementedException()
    End Function

    Public Overrides Function DeleteUser(username As String, deleteAllRelatedData As Boolean) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property EnablePasswordReset As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property EnablePasswordRetrieval As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Function FindUsersByEmail(emailToMatch As String,
                                              pageIndex As Integer,
                                              pageSize As Integer,
                                              ByRef totalRecords As Integer) As MembershipUserCollection
        Throw New NotImplementedException()
    End Function

    Public Overrides Function FindUsersByName(usernameToMatch As String,
                                             pageIndex As Integer,
                                             pageSize As Integer,
                                             ByRef totalRecords As Integer) As MembershipUserCollection
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetAllUsers(pageIndex As Integer, pageSize As Integer, ByRef totalRecords As Integer) As MembershipUserCollection
        Dim authSection As AuthenticationSection = CType(WebConfigurationManager.GetWebApplicationSection("system.web/authentication"), AuthenticationSection)
        Dim emailsSection As NameValueCollection = CType(System.Configuration.ConfigurationManager.GetSection("emails"), NameValueCollection)
        totalRecords = authSection.Forms.Credentials.Users.Count
        Dim users As MembershipUserCollection = New MembershipUserCollection()
        For i As Integer = pageIndex * pageSize To Math.Min(totalRecords, pageIndex * pageSize + pageSize) - 1
            Dim user As FormsAuthenticationUser = authSection.Forms.Credentials.Users(i)
            Dim email As String = Nothing
            email = emailsSection(user.Name)
            users.Add(New MembershipUser("FormsProvider",
                                        user.Name,
                                        Nothing,
                                        email,
                                        Nothing,
                                        Nothing,
                                        True,
                                        False,
                                        New DateTime(2000, 1, 1),
                                        New DateTime(2000, 1, 1),
                                        New DateTime(2000, 1, 1),
                                        New DateTime(2000, 1, 1),
                                        New DateTime(2000, 1, 1)))
        Next

        Return users
    End Function

    Public Overrides Function GetNumberOfUsersOnline() As Integer
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetPassword(username As String, answer As String) As String
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetUser(username As String, userIsOnline As Boolean) As MembershipUser
        Dim email As String = Nothing
        Dim authSection As AuthenticationSection = CType(WebConfigurationManager.GetWebApplicationSection("system.web/authentication"), AuthenticationSection)
        Dim user As FormsAuthenticationUser = authSection.Forms.Credentials.Users(username.ToLower())
        If user IsNot Nothing Then
            Dim emailsSection As NameValueCollection = CType(System.Configuration.ConfigurationManager.GetSection("emails"), NameValueCollection)
            email = emailsSection(user.Name)
            Return New MembershipUser("FormsProvider",
                                     username,
                                     Nothing,
                                     email,
                                     Nothing,
                                     Nothing,
                                     True,
                                     False,
                                     New DateTime(2000, 1, 1),
                                     New DateTime(2000, 1, 1),
                                     New DateTime(2000, 1, 1),
                                     New DateTime(2000, 1, 1),
                                     New DateTime(2000, 1, 1))
        End If

        Return Nothing
    End Function

    Public Overrides Function GetUser(providerUserKey As Object, userIsOnline As Boolean) As MembershipUser
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetUserNameByEmail(email As String) As String
        Throw New NotImplementedException()
    End Function

    Public Overrides ReadOnly Property MaxInvalidPasswordAttempts As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property MinRequiredNonAlphanumericCharacters As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property MinRequiredPasswordLength As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property PasswordAttemptWindow As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property PasswordFormat As MembershipPasswordFormat
        Get
            Return MembershipPasswordFormat.Clear
        End Get
    End Property

    Public Overrides ReadOnly Property PasswordStrengthRegularExpression As String
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property RequiresQuestionAndAnswer As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides ReadOnly Property RequiresUniqueEmail As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Function ResetPassword(username As String, answer As String) As String
        Throw New NotImplementedException()
    End Function

    Public Overrides Function UnlockUser(userName As String) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Sub UpdateUser(user As MembershipUser)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Function ValidateUser(username As String, password As String) As Boolean
        Return FormsAuthentication.Authenticate(username, password)
    End Function
End Class

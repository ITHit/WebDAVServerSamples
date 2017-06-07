
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace CalDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Implementation of membership provider which takes user names and passwords from standard section 
    /// in web.config/app.config.
    /// </summary>
    /// <remarks>
    /// It can be replaced with any other existing membership provider in web.config/app.config.
    /// </remarks>
    public class FormsMembershipProvider : MembershipProvider
    {
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(
            string username,
            string password,
            string newPasswordQuestion,
            string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(
            string username,
            string password,
            string email,
            string passwordQuestion,
            string passwordAnswer,
            bool isApproved,
            object providerUserKey,
            out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipUserCollection FindUsersByEmail(
            string emailToMatch,
            int pageIndex,
            int pageSize,
            out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(
            string usernameToMatch,
            int pageIndex,
            int pageSize,
            out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            AuthenticationSection authSection =
                (AuthenticationSection)WebConfigurationManager.GetWebApplicationSection("system.web/authentication");

            NameValueCollection emailsSection =
                (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("emails");

            totalRecords = authSection.Forms.Credentials.Users.Count;
            MembershipUserCollection users = new MembershipUserCollection();


            for (int i = pageIndex * pageSize;
                i < Math.Min(totalRecords, pageIndex * pageSize + pageSize);
                i++)
            {
                FormsAuthenticationUser user = authSection.Forms.Credentials.Users[i];
                string email = null;
                email = emailsSection[user.Name];

                users.Add(new MembershipUser(
                        "FormsProvider",
                        user.Name,
                        null,
                        email,
                        null,
                        null,
                        true,
                        false,
                        // do not use DateTime.MinValue because some WebDAV clients may not properly parse it.
                        new DateTime(2000, 1, 1),
                        new DateTime(2000, 1, 1),
                        new DateTime(2000, 1, 1),
                        new DateTime(2000, 1, 1),
                        new DateTime(2000, 1, 1))
                );
            }

            return users;
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            string email = null;

            AuthenticationSection authSection =
                (AuthenticationSection)WebConfigurationManager.GetWebApplicationSection("system.web/authentication");
            FormsAuthenticationUser user = authSection.Forms.Credentials.Users[username.ToLower()];

            if (user != null)
            {
                NameValueCollection emailsSection =
                    (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("emails");
                email = emailsSection[user.Name];
                return new MembershipUser(
                    "FormsProvider",
                    username,
                    null,
                    email,
                    null,
                    null,
                    true,
                    false,
                    // do not use DateTime.MinValue because some WebDAV clients may not properly parse it.
                    new DateTime(2000, 1, 1),
                    new DateTime(2000, 1, 1),
                    new DateTime(2000, 1, 1),
                    new DateTime(2000, 1, 1),
                    new DateTime(2000, 1, 1));
            }

            return null;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Clear; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
#pragma warning disable 0618
            return FormsAuthentication.Authenticate(username, password);
#pragma warning restore 0618
        }
    }
}

using System.Drawing;
using Microsoft.AspNetCore.Http;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.MSOFBAuthentication
{
    /// <summary>
    /// Provides options to configure MSOFBA Authentication Middleware.
    /// </summary>
    public class MSOFBAuthenticationConfig
    {
        /// <summary>
        /// Specifies the path to log-in page used by MS-OFBA.
        /// </summary>
        /// <remarks>
        /// This page will be displyed in dialog box presented by Microsoft Office 
        /// or other OFBA-enabled application.
        /// </remarks>
        public PathString LoginPath { get; set; }

        /// <summary>
        /// Specifies the path to log-in redirect URL which is used to indicate successful login
        /// </summary>
        /// <remarks>
        /// On seeing the redirect, the client determines that this URI matches that returned in response to the 
        /// Protocol Discovery request. In case the URIs match, the client assumes success, follows the 
        /// redirect, and closes the form.
        /// </remarks>
        public PathString LoginSuccessPath { get; set; }

        /// <summary>
        /// Gets or sets the size of the OFBA log-in dialog.
        /// </summary>
        /// <value>
        /// The size of the dialog.
        /// </value>
        public Size DialogSize { get; set; }

        /// <summary>
        /// The parameter name used to pass the return URL.
        /// </summary>
        /// <value>String that represents return URL parameter name.</value>
        public string ReturnUrlParameter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSOFBAuthenticationConfig"/> class.
        /// </summary>
        public MSOFBAuthenticationConfig()
            : base()
        {
        }
    }
}

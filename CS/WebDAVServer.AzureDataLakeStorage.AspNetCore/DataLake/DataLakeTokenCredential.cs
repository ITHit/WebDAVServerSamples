using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake
{
    public class DataLakeTokenCredential : TokenCredential
    {
        private readonly string token;
        private readonly DateTimeOffset expiresOn;

        public DataLakeTokenCredential(string token, DateTimeOffset expiresOn)
        {
            this.token = token;
            this.expiresOn = expiresOn;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(token, expiresOn);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken(token, expiresOn));
        }
    }
}

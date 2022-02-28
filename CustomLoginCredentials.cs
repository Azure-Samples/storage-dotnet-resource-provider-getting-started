using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace AzureStorageNew
{
    public class CustomLoginCredentials : ServiceClientCredentials
    {
        private string AuthenticationToken { get; set; }

        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            var authenticationContext = new AuthenticationContext("https://login.windows.net/" + "Tenet");
            var credential = new ClientCredential(clientId: "ClientID", clientSecret: "ClientSecret");
            var result = authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential);
            if (result == null) throw new InvalidOperationException("Failed to obtain the JWT token");
            AuthenticationToken = result.Result.AccessToken;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (AuthenticationToken == null) throw new InvalidOperationException("Token Provider Cannot Be Null");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //request.Version = new Version(apiVersion);
            await base.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}

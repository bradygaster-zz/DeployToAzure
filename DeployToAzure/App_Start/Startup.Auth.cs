using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using System.Configuration;
using DeployToAzure.DAL;
using System.IdentityModel.Claims;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using DeployToAzure.Models;

namespace DeployToAzure
{

    public partial class Startup
    {
        private DeployToAzureWebAppContext db = new DeployToAzureWebAppContext();
        public void ConfigureAuth(IAppBuilder app)
        {         
            string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
            string appKey = ConfigurationManager.AppSettings["ida:Password"];
            string graphResourceID = "https://graph.windows.net";
            //fixed address for multitenant apps in the public cloud
            string Authority = "https://login.windows.net/common/";

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthorizationCodeReceived = (context) =>
                       {
                           var code = context.Code;

                           ClientCredential credential = new ClientCredential(clientId, appKey);
                           string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                           string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                           AuthenticationContext authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", tenantID), new EFADALTokenCache(signedInUserID));
                           AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                               code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceID);

                           return Task.FromResult(0);
                       },
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example)without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                            context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                            context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.OwinContext.Response.Redirect("/Home/Error");
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });

        }

    }
}
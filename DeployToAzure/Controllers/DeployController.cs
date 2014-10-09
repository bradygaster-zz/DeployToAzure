using DeployToAzure.DAL;
using DeployToAzure.Hubs;
using DeployToAzure.Models;
using DeployToAzure.Web;
using LibGit2Sharp;
using Microsoft.AspNet.SignalR;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DeployToAzure.Controllers
{
    [System.Web.Mvc.Authorize]
    public class DeployController : Controller
    {
        string TenantId
        {
            get { return ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value; }
        }

        string GetAccessToken()
        {
            string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
            string appKey = ConfigurationManager.AppSettings["ida:Password"];
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            try
            {
                // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
                ClientCredential clientcred = new ClientCredential(clientId, appKey);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}",
                    TenantId), new EFADALTokenCache(signedInUserID));
                AuthenticationResult result = authContext.AcquireTokenSilent("https://management.core.windows.net/",
                    clientcred, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                return result.AccessToken;
            }
            // if the above failed, the user needs to explicitly re-authenticate for the app to obtain the required token
            catch (Exception ee)
            {
                return null;
            }
        }

        void LogToSignalR(string message, string webSiteName)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<StatusHub>();
            hub.Clients.Group(webSiteName).messageTraced(message);
        }

        List<SubscriptionViewModel> GetSubscriptions()
        {
            using (var subscriptionClient = CloudContext.Clients.CreateSubscriptionClient(
                new TokenCloudCredentials(GetAccessToken())
                ))
            {
                var result = subscriptionClient.Subscriptions.List();

                return result.Subscriptions.Select(x => new SubscriptionViewModel
                {
                    Name = x.SubscriptionName,
                    SubscriptionId = x.SubscriptionId,
                    ActiveDirectoryTenantId = x.ActiveDirectoryTenantId
                }).ToList();
            }
        }

        List<WebspaceViewModel> GetWebSpaces(string accessToken, string subscriptionId)
        {
            var ret = new List<WebspaceViewModel>();

            using (var webSiteManagementClient = CloudContext.Clients.CreateWebSiteManagementClient(
                    new TokenCloudCredentials(subscriptionId, accessToken)
                    ))
            {
                var webSpaceListResult = webSiteManagementClient.WebSpaces.List();

                webSpaceListResult.WebSpaces.ToList().ForEach(x => ret.Add(
                    new WebspaceViewModel
                    {
                        WebSpaceName = x.Name,
                        GeoRegion = x.GeoRegion
                    }));
            }

            return ret;
        }

        string CloneSiteRepository(string gitUrl, string webSiteName)
        {
            var localGitRepoDirName =
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            var srcFolder =
                Server.MapPath(string.Format("~/App_Data/{0}", localGitRepoDirName));

            LogToSignalR("Cloning repository", webSiteName);

            if (Directory.Exists(srcFolder))
                Directory.Delete(srcFolder, true);

            Repository.Clone(gitUrl, srcFolder);

            LogToSignalR("Cloned repository", webSiteName);

            return srcFolder;
        }

        // todo: clean up the parameters with an object
        void CreateAndDeployWebsite(string webSpaceName,
            string webSiteName,
            string webSiteSrcFolder,
            string subscriptionId,
            string accessToken)
        {
            using (var webSiteManagementClient = CloudContext.Clients.CreateWebSiteManagementClient(
                new TokenCloudCredentials(subscriptionId, accessToken)
                ))
            {
                var hostingPlanListResult = webSiteManagementClient.WebHostingPlans.List(webSpaceName);

                if (hostingPlanListResult.WebHostingPlans.Count == 0)
                {
                    webSiteManagementClient.WebHostingPlans.Create(webSpaceName,
                        new WebHostingPlanCreateParameters
                        {
                            Name = "QuickDeployed"
                        });
                }

                hostingPlanListResult = webSiteManagementClient.WebHostingPlans.List(webSpaceName);

                LogToSignalR("Creating your web site", webSiteName);

                var webSiteCreateResult = webSiteManagementClient.WebSites.Create(
                    webSpaceName,
                    new WebSiteCreateParameters
                    {
                        Name = webSiteName,
                        ServerFarm = hostingPlanListResult.WebHostingPlans.First().Name
                    });

                LogToSignalR("Getting site publishing settings", webSiteName);

                var profiles = webSiteManagementClient.WebSites.GetPublishProfile(webSpaceName, webSiteName);
                var webDeployProfile = profiles.First(x => x.MSDeploySite != null);

                if (Directory.GetFiles(webSiteSrcFolder, "deploytoazure.json").Any())
                {
                    var json = new StreamReader(System.IO.File.OpenRead(string.Format("{0}\\deploytoazure.json", webSiteSrcFolder))).ReadToEnd();
                    var deploymentModel = JsonConvert.DeserializeObject<DeployToModel>(json);
                    if (!string.IsNullOrEmpty(deploymentModel.SubdirectoryWithWebsite))
                    {
                        webSiteSrcFolder = string.Format("{0}\\{1}", webSiteSrcFolder, deploymentModel.SubdirectoryWithWebsite);
                    }
                }

                LogToSignalR("Publishing your site", webSiteName);

                new WebDeployPublishingHelper(
                    webDeployProfile.PublishUrl,
                    webDeployProfile.MSDeploySite,
                    webDeployProfile.UserName,
                    webDeployProfile.UserPassword,
                    webSiteSrcFolder
                    )
                    .PublishFolder();

                LogToSignalR("All finished!", webSiteName);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var token = GetAccessToken();

            // todo: add better error handling around this next line
            var activeSubscription = GetSubscriptions().First(x => x.ActiveDirectoryTenantId == TenantId);
            var webSpaces = GetWebSpaces(token, activeSubscription.SubscriptionId);
            var gitRepoUrl = string.Empty;

            if (string.IsNullOrEmpty(token))
            {
                // todo: let the user know they need to re-auth to get a new token
            }

            // think about this logic, it isn't bullet-proof...
            if (!string.IsNullOrEmpty(Request.ServerVariables["HTTP_REFERER"]))
                gitRepoUrl = string.Format("{0}.git", Request.ServerVariables["HTTP_REFERER"]);
            else
            {
                if (string.IsNullOrEmpty(Request.QueryString["giturl"]))
                    gitRepoUrl = Request.QueryString["giturl"];
            }

            return View(new DeployViewModel
            {
                AccessToken = token,
                Subscription = activeSubscription,
                WebSpaces = webSpaces,
                GitRepositoryUrl = gitRepoUrl
            });
        }

        // todo: clean up the parameters with an object
        [HttpPost]
        public JsonResult Index(string webSpace,
            string gitUrl,
            string webSiteName)
        {
            var token = GetAccessToken();
            var activeSubscription = GetSubscriptions().First(x => x.ActiveDirectoryTenantId == TenantId);

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    // todo: let the user know they need to re-auth to get a new token
                }

                var sourceFolder = this.CloneSiteRepository(gitUrl, webSiteName);

                this.CreateAndDeployWebsite(webSpace,
                    webSiteName,
                    sourceFolder,
                    activeSubscription.SubscriptionId,
                    token);
            }
            catch (Exception ex)
            {

            }

            return Json(new
            {
                success = true,
                siteUrl = string.Format("http://{0}.azurewebsites.net", webSiteName)
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CheckWebSiteName(string webSiteName)
        {
            var token = GetAccessToken();
            var activeSubscription = GetSubscriptions().First(x => x.ActiveDirectoryTenantId == TenantId);
            var isSiteAvailable = false;

            using (var webSiteMgmtClient = CloudContext.Clients.CreateWebSiteManagementClient(
                new TokenCloudCredentials(activeSubscription.SubscriptionId, token)
                ))
            {
                var result = webSiteMgmtClient.WebSites.IsHostnameAvailable(webSiteName);
                isSiteAvailable = result.IsAvailable;
            }

            return Json(new
            {
                isSiteAvailable = isSiteAvailable
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
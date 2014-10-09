using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DeployToAzure.Models
{
    public class DeployViewModel
    {
        public DeployViewModel()
        {
            this.WebSpaces = new List<WebspaceViewModel>();
        }

        public string AccessToken { get; set; }

        public SubscriptionViewModel Subscription { get; set; }

        public List<WebspaceViewModel> WebSpaces { get; set; }

        public string GitRepositoryUrl { get; set; }
    }

    public class SubscriptionViewModel
    {
        public string SubscriptionId { get; set; }
        public string Name { get; set; }
        public string ActiveDirectoryTenantId { get; set; }
    }

    public class WebspaceViewModel
    {
        public string GeoRegion { get; set; }
        public string WebSpaceName { get; set; }
    }

    public class DeployToModel
    {
        [DataMember(Name = "subdirectoryWithWebsite")]
        public string SubdirectoryWithWebsite { get; set; }
    }
}
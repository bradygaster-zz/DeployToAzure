﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DeployToAzure.Models
{
    // Entity for keeping track of organizations onboarded as customers of the app
    public class Tenant
    {
        public Tenant()
        {
            this.AdminConsented = true;
        }

        public int ID { get; set; }
        public string IssValue { get; set; }
        public DateTime Created { get; set; }
        [DisplayName("Check this if you are an administrator and you want to enable the app for all your users")]
        public bool AdminConsented { get; set; }
    }

    // Entity for keeping track of individual users onboarded as customers of the app
    public class User
    {
        [Key]
        public string UPN { get; set; }
        public string TenantID { get; set; }
    }

    // Entity for saving tokens for accessing API
    public class TokenCacheEntry
    {
        public int ID { get; set; }
        public string SignedInUser { get; set; }
        public string TokenRequestorUser { get; set; } 
        public string ResourceID { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset Expiration { get; set; }
    }
}
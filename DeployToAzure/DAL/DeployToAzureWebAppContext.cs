using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using DeployToAzure.Models;

namespace DeployToAzure.DAL
{
   
    public class DeployToAzureWebAppContext : DbContext
    {
        public DeployToAzureWebAppContext()
            : base("DeployToAzureWebAppContext")
        { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PerWebUserCache> PerUserCacheList { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
namespace DeployToAzure.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<DeployToAzure.DAL.DeployToAzureWebAppContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(DeployToAzure.DAL.DeployToAzureWebAppContext context)
        {
        }
    }
}

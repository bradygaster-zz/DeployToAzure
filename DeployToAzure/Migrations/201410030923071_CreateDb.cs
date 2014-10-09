namespace DeployToAzure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PerWebUserCache",
                c => new
                    {
                        EntryId = c.Int(nullable: false, identity: true),
                        webUserUniqueId = c.String(),
                        cacheBits = c.Binary(),
                        LastWrite = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.EntryId);
            
            CreateTable(
                "dbo.Tenant",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        IssValue = c.String(),
                        Created = c.DateTime(nullable: false),
                        AdminConsented = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.User",
                c => new
                    {
                        UPN = c.String(nullable: false, maxLength: 128),
                        TenantID = c.String(),
                    })
                .PrimaryKey(t => t.UPN);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.User");
            DropTable("dbo.Tenant");
            DropTable("dbo.PerWebUserCache");
        }
    }
}

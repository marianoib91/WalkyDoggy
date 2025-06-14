namespace WalkyDoggy.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class v1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Breeds",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Cities",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        ProvinceId = c.Long(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        PostalCode = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Provinces", t => t.ProvinceId, cascadeDelete: false)
                .Index(t => t.ProvinceId);
            
            CreateTable(
                "dbo.Provinces",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Customers",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.Long(nullable: false),
                        CityId = c.Long(nullable: false),
                        FirstName = c.String(nullable: false, maxLength: 100),
                        LastName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 200),
                        Phone = c.String(nullable: false, maxLength: 50),
                        StreetName = c.String(nullable: false, maxLength: 50),
                        StreetNumber = c.Long(nullable: false),
                        ProfileImage = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Cities", t => t.CityId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.UserId)
                .Index(t => t.CityId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Email = c.String(nullable: false, maxLength: 200),
                        HashedPassword = c.String(nullable: false, maxLength: 200),
                        Salt = c.String(nullable: false, maxLength: 200),
                        IsLocked = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Errors",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Message = c.String(),
                        StackTrace = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Pets",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        CustomerId = c.Long(nullable: false),
                        SizeId = c.Long(nullable: false),
                        BreedId = c.Long(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        Age = c.Long(nullable: false),
                        Description = c.String(maxLength: 150),
                        ProfileImage = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Breeds", t => t.BreedId, cascadeDelete: false)
                .ForeignKey("dbo.Customers", t => t.CustomerId, cascadeDelete: false)
                .ForeignKey("dbo.Sizes", t => t.SizeId, cascadeDelete: false)
                .Index(t => t.CustomerId)
                .Index(t => t.SizeId)
                .Index(t => t.BreedId);
            
            CreateTable(
                "dbo.Sizes",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Prices",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Amount = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Rankings",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        WalkId = c.Long(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Score = c.Double(nullable: false),
                        Comments = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Walks", t => t.WalkId, cascadeDelete: false)
                .Index(t => t.WalkId);
            
            CreateTable(
                "dbo.Walks",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        WalkerId = c.Long(nullable: false),
                        PetId = c.Long(nullable: false),
                        PriceId = c.Long(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Details = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Pets", t => t.PetId, cascadeDelete: false)
                .ForeignKey("dbo.Prices", t => t.PriceId, cascadeDelete: false)
                .ForeignKey("dbo.Walkers", t => t.WalkerId, cascadeDelete: false)
                .Index(t => t.WalkerId)
                .Index(t => t.PetId)
                .Index(t => t.PriceId);
            
            CreateTable(
                "dbo.Walkers",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.Long(nullable: false),
                        CityId = c.Long(nullable: false),
                        FirstName = c.String(nullable: false, maxLength: 100),
                        LastName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 200),
                        Description = c.String(nullable: false, maxLength: 200),
                        Phone = c.String(nullable: false, maxLength: 50),
                        StreetName = c.String(nullable: false, maxLength: 50),
                        StreetNumber = c.Long(nullable: false),
                        ProfileImage = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Cities", t => t.CityId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.UserId)
                .Index(t => t.CityId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.Long(nullable: false),
                        RoleId = c.Long(nullable: false)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Roles", t => t.RoleId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.RoleId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserRoles", "User_Id", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "Role_Id", "dbo.Roles");
            DropForeignKey("dbo.Rankings", "WalkId", "dbo.Walks");
            DropForeignKey("dbo.Walks", "WalkerId", "dbo.Walkers");
            DropForeignKey("dbo.Walkers", "UserId", "dbo.Users");
            DropForeignKey("dbo.Walkers", "CityId", "dbo.Cities");
            DropForeignKey("dbo.Walks", "PriceId", "dbo.Prices");
            DropForeignKey("dbo.Walks", "PetId", "dbo.Pets");
            DropForeignKey("dbo.Pets", "SizeId", "dbo.Sizes");
            DropForeignKey("dbo.Pets", "CustomerId", "dbo.Customers");
            DropForeignKey("dbo.Pets", "BreedId", "dbo.Breeds");
            DropForeignKey("dbo.Customers", "UserId", "dbo.Users");
            DropForeignKey("dbo.Customers", "CityId", "dbo.Cities");
            DropForeignKey("dbo.Cities", "ProvinceId", "dbo.Provinces");
            DropIndex("dbo.UserRoles", new[] { "User_Id" });
            DropIndex("dbo.UserRoles", new[] { "Role_Id" });
            DropIndex("dbo.Walkers", new[] { "CityId" });
            DropIndex("dbo.Walkers", new[] { "UserId" });
            DropIndex("dbo.Walks", new[] { "PriceId" });
            DropIndex("dbo.Walks", new[] { "PetId" });
            DropIndex("dbo.Walks", new[] { "WalkerId" });
            DropIndex("dbo.Rankings", new[] { "WalkId" });
            DropIndex("dbo.Pets", new[] { "BreedId" });
            DropIndex("dbo.Pets", new[] { "SizeId" });
            DropIndex("dbo.Pets", new[] { "CustomerId" });
            DropIndex("dbo.Customers", new[] { "CityId" });
            DropIndex("dbo.Customers", new[] { "UserId" });
            DropIndex("dbo.Cities", new[] { "ProvinceId" });
            DropTable("dbo.UserRoles");
            DropTable("dbo.Roles");
            DropTable("dbo.Walkers");
            DropTable("dbo.Walks");
            DropTable("dbo.Rankings");
            DropTable("dbo.Prices");
            DropTable("dbo.Sizes");
            DropTable("dbo.Pets");
            DropTable("dbo.Errors");
            DropTable("dbo.Users");
            DropTable("dbo.Customers");
            DropTable("dbo.Provinces");
            DropTable("dbo.Cities");
            DropTable("dbo.Breeds");
        }
    }
}

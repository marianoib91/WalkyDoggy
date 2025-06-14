namespace WalkyDoggy.Data.Migrations
{
    using WalkyDoggy.Entities;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Migrations;
    using WalkyDoggy.Data;

    //Enable-Migrations 
    //Add-Migration
    //Update-Database or Update-Database –Verbose
    //Re-iniciar: Update-Database -TargetMigration:0 -force

    internal sealed class Configuration : DbMigrationsConfiguration<WalkyDoggyContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(WalkyDoggyContext context)
        {


        }
    }
}

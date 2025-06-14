using WalkyDoggy.Data.Configurations;
using WalkyDoggy.Entities;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using WalkyDoggy.Entities.Entities;

namespace WalkyDoggy.Data
{
    public class WalkyDoggyContext : DbContext
    {
        public WalkyDoggyContext()
            : base("WalkyDoggy")
        {
            Database.SetInitializer<WalkyDoggyContext>(null);
        }

        #region Entity Sets
        public IDbSet<User> Users { get; set; }
        public IDbSet<Walker> Walkers { get; set; }
        public IDbSet<Customer> Customers { get; set; }
        public IDbSet<Pet> Pets { get; set; }
        public IDbSet<Walk> Walks { get; set; }
        public IDbSet<WorkDay> WorkDays { get; set; }
        public IDbSet<Ranking> Rankings { get; set; }
        public IDbSet<Breed> Breeds { get; set; }
        public IDbSet<Size> Sizes { get; set; }
        public IDbSet<Price> Prices { get; set; }
        public IDbSet<City> Cities { get; set; }
        public IDbSet<Province> Provinces { get; set; }
        public IDbSet<UserRole> UserRoles { get; set; }
        public IDbSet<Role> Roles { get; set; }
        public IDbSet<Error> Errors { get; set; }
        #endregion

        public virtual void Commit()
        {
            base.SaveChanges();
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Configurations.Add(new UsersConfiguration());
            modelBuilder.Configurations.Add(new CustomersConfiguration());
            modelBuilder.Configurations.Add(new WalkersConfiguration());
            modelBuilder.Configurations.Add(new WalksConfiguration());
            modelBuilder.Configurations.Add(new PetsConfiguration());
            modelBuilder.Configurations.Add(new BreedsConfiguration());
            modelBuilder.Configurations.Add(new SizesConfiguration());
            modelBuilder.Configurations.Add(new CitiesConfiguration());
            modelBuilder.Configurations.Add(new ProvincesConfiguration());
            modelBuilder.Configurations.Add(new RankingsConfiguration());
            modelBuilder.Configurations.Add(new PricesConfiguration());
            modelBuilder.Configurations.Add(new WorkDaysConfiguration());


        }
    }
}

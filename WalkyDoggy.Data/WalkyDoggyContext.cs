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

        #region Conjuntos de entidades
        public IDbSet<User> Users { get; set; }
        public IDbSet<Walker> Walkers { get; set; }
        public IDbSet<Customer> Customers { get; set; }
        public IDbSet<Pet> Pets { get; set; }
        public IDbSet<Walk> Walks { get; set; }
        public IDbSet<WorkDay> WorkDays { get; set; }
        public IDbSet<Ranking> Rankings { get; set; }
        public IDbSet<Message> Messages { get; set; }
        public IDbSet<FavoriteWalker> FavoriteWalkers { get; set; }
        public IDbSet<PetReview> PetReviews { get; set; }
        public IDbSet<AdminAction> AdminActions { get; set; }
        public IDbSet<Advertiser> Advertisers { get; set; }
        public IDbSet<Ad> Ads { get; set; }
        public IDbSet<Complaint> Complaints { get; set; }
        public IDbSet<ComplaintImage> ComplaintImages { get; set; }
        public IDbSet<PetTraitDefinition> PetTraitDefinitions { get; set; }
        public IDbSet<Breed> Breeds { get; set; }
        public IDbSet<Size> Sizes { get; set; }
        public IDbSet<Price> Prices { get; set; }
        public IDbSet<City> Cities { get; set; }
        public IDbSet<Province> Provinces { get; set; }
        public IDbSet<UserRole> UserRoles { get; set; }
        public IDbSet<Role> Roles { get; set; }
        public IDbSet<Error> Errors { get; set; }
        #endregion

        public virtual void GuardarCambios()
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
            modelBuilder.Configurations.Add(new MessagesConfiguration());
            modelBuilder.Configurations.Add(new FavoriteWalkersConfiguration());
            modelBuilder.Configurations.Add(new PetReviewsConfiguration());
            modelBuilder.Configurations.Add(new AdminActionsConfiguration());
            modelBuilder.Configurations.Add(new AdvertisersConfiguration());
            modelBuilder.Configurations.Add(new AdsConfiguration());
            modelBuilder.Configurations.Add(new ComplaintsConfiguration());
            modelBuilder.Configurations.Add(new ComplaintImagesConfiguration());
            modelBuilder.Configurations.Add(new PetTraitDefinitionsConfiguration());
            modelBuilder.Configurations.Add(new PricesConfiguration());
            modelBuilder.Configurations.Add(new WorkDaysConfiguration());


        }
    }
}

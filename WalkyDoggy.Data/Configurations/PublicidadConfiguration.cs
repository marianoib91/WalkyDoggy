using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class AdvertisersConfiguration : EntityBaseConfiguration<Advertiser>
    {
        public AdvertisersConfiguration()
        {
            Property(a => a.Name).IsRequired().HasMaxLength(80);
            Property(a => a.Category).IsRequired().HasMaxLength(20);
            Property(a => a.Description).HasMaxLength(300);
            Property(a => a.Phone).HasMaxLength(30);
            Property(a => a.Website).HasMaxLength(200);
            Property(a => a.StreetName).HasMaxLength(100);
            Property(a => a.CityName).HasMaxLength(100);
            Property(a => a.Latitude).HasMaxLength(30);
            Property(a => a.Longitude).HasMaxLength(30);
            Property(a => a.Active).IsRequired();
            Property(a => a.CreatedAt).IsRequired();
        }
    }

    public class AdsConfiguration : EntityBaseConfiguration<Ad>
    {
        public AdsConfiguration()
        {
            Property(a => a.AdvertiserId).IsRequired();
            Property(a => a.Title).IsRequired().HasMaxLength(60);
            Property(a => a.Text).HasMaxLength(200);
            Property(a => a.ImageFile).HasMaxLength(80);
            Property(a => a.LinkUrl).HasMaxLength(200);
            Property(a => a.StartDate).IsRequired();
            Property(a => a.EndDate).IsRequired();
            Property(a => a.Audience).IsRequired().HasMaxLength(10);
            Property(a => a.Active).IsRequired();
            Property(a => a.Impressions).IsRequired();
            Property(a => a.Clicks).IsRequired();
            Property(a => a.CreatedAt).IsRequired();
        }
    }
}

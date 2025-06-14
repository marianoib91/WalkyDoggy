using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class CitiesConfiguration : EntityBaseConfiguration<City>
    {
        public CitiesConfiguration()
        {
            Property(u => u.ProvinceId).IsRequired();
            Property(u => u.Name).IsRequired().HasMaxLength(100);
            Property(u => u.PostalCode).IsRequired();
        }
    }
}

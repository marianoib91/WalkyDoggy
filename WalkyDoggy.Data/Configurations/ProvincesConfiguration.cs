using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class ProvincesConfiguration : EntityBaseConfiguration<Province>
    {
        public ProvincesConfiguration()
        {
            Property(u => u.Name).IsRequired().HasMaxLength(100);
        }
    }
}

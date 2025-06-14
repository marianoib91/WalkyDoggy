using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class BreedsConfiguration : EntityBaseConfiguration<Breed>
    {
        public BreedsConfiguration()
        {
            Property(u => u.Name).IsRequired().HasMaxLength(100);
        }
    }
}

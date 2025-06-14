using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class PetsConfiguration : EntityBaseConfiguration<Pet>
    {
        public PetsConfiguration()
        {
            Property(u => u.CustomerId).IsRequired();
            Property(u => u.BreedId).IsRequired();
            Property(u => u.SizeId).IsRequired();
            Property(u => u.Name).IsRequired().HasMaxLength(100);
            Property(u => u.Age).IsRequired();
            Property(u => u.Description).HasMaxLength(150);
            Property(u => u.ProfileImage);
        }
    }
}

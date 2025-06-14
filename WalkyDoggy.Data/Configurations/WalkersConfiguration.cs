using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class WalkersConfiguration : EntityBaseConfiguration<Walker>
    {
        public WalkersConfiguration()
        {
            Property(u => u.CityId).IsRequired();
            Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            Property(u => u.LastName).IsRequired().HasMaxLength(100);
            Property(u => u.Email).IsRequired().HasMaxLength(200);
            Property(u => u.Description).IsRequired().HasMaxLength(200);
            Property(u => u.Phone).IsRequired().HasMaxLength(50);
            Property(u => u.StreetName).IsRequired().HasMaxLength(50);
            Property(u => u.StreetNumber).IsRequired();
            Property(u => u.UserId).IsRequired();
            Property(u => u.ProfileImage);
        }
    }
}

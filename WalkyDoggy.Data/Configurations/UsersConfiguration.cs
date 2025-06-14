using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class UsersConfiguration : EntityBaseConfiguration<User>
    {
        public UsersConfiguration()
        {
            Property(u => u.Email).IsRequired().HasMaxLength(200);
            Property(u => u.HashedPassword).IsRequired().HasMaxLength(200);
            Property(u => u.Salt).IsRequired().HasMaxLength(200);
        }
    }
}

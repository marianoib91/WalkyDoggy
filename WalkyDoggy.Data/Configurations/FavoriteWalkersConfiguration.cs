using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class FavoriteWalkersConfiguration : EntityBaseConfiguration<FavoriteWalker>
    {
        public FavoriteWalkersConfiguration()
        {
            Property(u => u.CustomerId).IsRequired();
            Property(u => u.WalkerId).IsRequired();
            Property(u => u.CreatedAt).IsRequired();
        }
    }
}

using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class RankingsConfiguration : EntityBaseConfiguration<Ranking>
    {
        public RankingsConfiguration()
        {
            Property(u => u.WalkerId).IsRequired();
            Property(u => u.CustomerId).IsRequired();
            Property(u => u.BookingKey).IsRequired().HasMaxLength(40);
            Property(u => u.Date).IsRequired();
            Property(u => u.Score).IsRequired();
            Property(u => u.Comments).HasMaxLength(500);
            Property(u => u.Hidden).IsRequired();
            Property(u => u.HiddenReason).HasMaxLength(300);
        }
    }
}

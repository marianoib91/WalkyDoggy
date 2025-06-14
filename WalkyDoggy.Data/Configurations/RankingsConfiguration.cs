using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class RankingsConfiguration : EntityBaseConfiguration<Ranking>
    {
        public RankingsConfiguration()
        {
            Property(u => u.WalkId).IsRequired();
            Property(u => u.Date).IsRequired();
            Property(u => u.Comments).IsRequired();
            Property(u => u.Score).IsRequired();
            Property(u => u.Comments).HasMaxLength(150);
        }
    }
}

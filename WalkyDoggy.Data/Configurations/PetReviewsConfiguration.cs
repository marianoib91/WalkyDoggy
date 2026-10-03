using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class PetReviewsConfiguration : EntityBaseConfiguration<PetReview>
    {
        public PetReviewsConfiguration()
        {
            Property(u => u.WalkId).IsRequired();
            Property(u => u.PetId).IsRequired();
            Property(u => u.WalkerId).IsRequired();
            Property(u => u.BookingKey).IsRequired().HasMaxLength(40);
            Property(u => u.Date).IsRequired();
            Property(u => u.Stars).IsRequired();
            Property(u => u.Comments).HasMaxLength(500);
        }
    }
}

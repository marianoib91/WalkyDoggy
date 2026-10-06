using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class StaysConfiguration : EntityBaseConfiguration<Stay>
    {
        public StaysConfiguration()
        {
            Property(u => u.WalkerId).IsRequired();
            Property(u => u.CustomerId).IsRequired();
            Property(u => u.CheckIn).IsRequired();
            Property(u => u.CheckOut).IsRequired();
            Property(u => u.PricePerNight).HasPrecision(10, 2);
            Property(u => u.Total).HasPrecision(12, 2);
            Property(u => u.Details).HasMaxLength(500);
            Property(u => u.Status).IsRequired().HasMaxLength(20);
            Property(u => u.PaymentMethod).IsRequired().HasMaxLength(20);
            Property(u => u.PaymentStatus).IsRequired().HasMaxLength(20);
            Property(u => u.PaymentId).HasMaxLength(60);
            Property(u => u.CancelledBy).HasMaxLength(20);
            HasMany(u => u.Pets).WithRequired().HasForeignKey(p => p.StayId);
        }
    }

    public class StayPetsConfiguration : EntityBaseConfiguration<StayPet>
    {
        public StayPetsConfiguration()
        {
            Property(u => u.StayId).IsRequired();
            Property(u => u.PetId).IsRequired();
        }
    }
}

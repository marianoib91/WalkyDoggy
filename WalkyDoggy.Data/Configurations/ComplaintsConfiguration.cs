using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class ComplaintsConfiguration : EntityBaseConfiguration<Complaint>
    {
        public ComplaintsConfiguration()
        {
            Property(c => c.BookingKey).IsRequired().HasMaxLength(40);
            Property(c => c.ReporterRole).IsRequired().HasMaxLength(10);
            Property(c => c.Reason).IsRequired().HasMaxLength(30);
            Property(c => c.Description).IsRequired().HasMaxLength(1000);
            Property(c => c.Status).IsRequired().HasMaxLength(15);
            Property(c => c.Resolution).HasMaxLength(15);
            Property(c => c.ResolutionNote).HasMaxLength(500);
            Property(c => c.CreatedAt).IsRequired();
        }
    }

    public class ComplaintImagesConfiguration : EntityBaseConfiguration<ComplaintImage>
    {
        public ComplaintImagesConfiguration()
        {
            Property(i => i.ComplaintId).IsRequired();
            Property(i => i.FileName).IsRequired().HasMaxLength(80);
            Property(i => i.OriginalName).HasMaxLength(200);
            Property(i => i.ContentType).IsRequired().HasMaxLength(40);
            Property(i => i.SizeBytes).IsRequired();
            Property(i => i.CreatedAt).IsRequired();
        }
    }
}

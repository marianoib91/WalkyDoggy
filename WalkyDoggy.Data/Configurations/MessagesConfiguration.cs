using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class MessagesConfiguration : EntityBaseConfiguration<Message>
    {
        public MessagesConfiguration()
        {
            Property(u => u.BookingKey).IsRequired().HasMaxLength(40);
            Property(u => u.SenderRole).IsRequired().HasMaxLength(10);
            Property(u => u.Text).IsRequired().HasMaxLength(500);
            Property(u => u.SentAt).IsRequired();
        }
    }
}

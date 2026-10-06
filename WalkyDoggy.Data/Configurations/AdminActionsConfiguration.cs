using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class AdminActionsConfiguration : EntityBaseConfiguration<AdminAction>
    {
        public AdminActionsConfiguration()
        {
            Property(a => a.AdminUserId).IsRequired();
            Property(a => a.Action).IsRequired().HasMaxLength(40);
            Property(a => a.Detail).HasMaxLength(1000);
            Property(a => a.CreatedAt).IsRequired();
        }
    }
}

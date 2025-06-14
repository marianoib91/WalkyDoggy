using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class SizesConfiguration : EntityBaseConfiguration<Size>
    {
        public SizesConfiguration()
        {

            Property(u => u.Name).IsRequired().HasMaxLength(100);
        }
    }
}

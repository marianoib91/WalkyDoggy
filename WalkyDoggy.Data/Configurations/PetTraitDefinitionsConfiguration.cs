using WalkyDoggy.Entities;

namespace WalkyDoggy.Data.Configurations
{
    public class PetTraitDefinitionsConfiguration : EntityBaseConfiguration<PetTraitDefinition>
    {
        public PetTraitDefinitionsConfiguration()
        {
            Property(t => t.Code).IsRequired().HasMaxLength(40);
            Property(t => t.Label).IsRequired().HasMaxLength(60);
            Property(t => t.PairId).IsRequired();
            Property(t => t.Position).IsRequired();
            Property(t => t.Active).IsRequired();
        }
    }
}

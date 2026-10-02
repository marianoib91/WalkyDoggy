namespace WalkyDoggy.Data.Infrastructure
{
    public class UnidadDeTrabajo : IUnidadDeTrabajo
    {
        private readonly IFabricaDb fabricaDb;
        private WalkyDoggyContext contextoDb;

        public UnidadDeTrabajo(IFabricaDb fabricaDb)
        {
            this.fabricaDb = fabricaDb;
        }

        public WalkyDoggyContext DbContext
        {
            get { return contextoDb ?? (contextoDb = fabricaDb.Iniciar()); }
        }

        public void GuardarCambios()
        {
            DbContext.GuardarCambios();
        }
    }
}

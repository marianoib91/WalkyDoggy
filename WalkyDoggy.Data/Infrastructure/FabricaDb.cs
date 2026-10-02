namespace WalkyDoggy.Data.Infrastructure
{
    public class FabricaDb : Disposable, IFabricaDb
    {
        WalkyDoggyContext contextoDb;

        public WalkyDoggyContext Iniciar()
        {
            return contextoDb ?? (contextoDb = new WalkyDoggyContext());
        }

        protected override void DisposeCore()
        {
            if (contextoDb != null)
                contextoDb.Dispose();
        }
    }
}

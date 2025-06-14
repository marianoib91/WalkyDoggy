namespace WalkyDoggy.Data.Infrastructure
{
    public class DbFactory : Disposable, IDbFactory
    {
        WalkyDoggyContext dbContext;

        public WalkyDoggyContext Init()
        {
            return dbContext ?? (dbContext = new WalkyDoggyContext());
        }

        protected override void DisposeCore()
        {
            if (dbContext != null)
                dbContext.Dispose();
        }
    }
}

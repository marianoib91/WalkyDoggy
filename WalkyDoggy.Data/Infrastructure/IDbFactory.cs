using System;

namespace WalkyDoggy.Data.Infrastructure
{
    public interface IDbFactory : IDisposable
    {
        WalkyDoggyContext Init();
    }
}

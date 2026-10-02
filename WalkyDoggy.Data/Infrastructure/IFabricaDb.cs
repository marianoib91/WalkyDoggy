using System;

namespace WalkyDoggy.Data.Infrastructure
{
    public interface IFabricaDb : IDisposable
    {
        WalkyDoggyContext Iniciar();
    }
}

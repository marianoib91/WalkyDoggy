using System;

namespace WalkyDoggy.Data.Infrastructure
{
    public class Disposable : IDisposable
    {
        private bool estaLiberado;

        ~Disposable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool liberando)
        {
            if (!estaLiberado && liberando)
            {
                DisposeCore();
            }

            estaLiberado = true;
        }

        // Sobrescribir para liberar objetos propios
        protected virtual void DisposeCore()
        {
        }
    }
}

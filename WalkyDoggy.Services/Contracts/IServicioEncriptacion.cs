using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Services
{
    public interface IServicioEncriptacion
    {
        /// <summary>
        /// Crea una sal aleatoria
        /// </summary>
        /// <returns></returns>
        string CrearSal();
        /// <summary>
        /// Genera la contrasena encriptada (hash)
        /// </summary>
        /// <param name="contrasena"></param>
        /// <param name="sal"></param>
        /// <returns></returns>
        string EncriptarContrasena(string contrasena, string sal);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Services
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Crea una sal aleatoria
        /// </summary>
        /// <returns></returns>
        string CreateSalt();
        /// <summary>
        /// Genera la contrasena encriptada (hash)
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        string EncryptPassword(string password, string salt);
    }
}

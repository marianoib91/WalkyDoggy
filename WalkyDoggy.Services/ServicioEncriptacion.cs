using System;
using System.Security.Cryptography;
using System.Text;

namespace WalkyDoggy.Services
{
    public class ServicioEncriptacion : IServicioEncriptacion
    {
        public string CrearSal()
        {
            var datos = new byte[0x10];
            using (var proveedorCriptografico = new RNGCryptoServiceProvider())
            {
                proveedorCriptografico.GetBytes(datos);
                return Convert.ToBase64String(datos);
            }
        }

        public string EncriptarContrasena(string contrasena, string sal)
        {
            using (var sha256 = SHA256.Create())
            {
                var contrasenaConSal = string.Format("{0}{1}", sal, contrasena);
                byte[] bytesContrasenaConSal = Encoding.UTF8.GetBytes(contrasenaConSal);
                return Convert.ToBase64String(sha256.ComputeHash(bytesContrasenaConSal));
            }
        }
    }
}

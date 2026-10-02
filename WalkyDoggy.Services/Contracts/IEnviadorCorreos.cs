using System;

namespace WalkyDoggy.Services.Contracts
{
    //Envio de mails. No debe lanzar excepciones: un mail que no sale nunca tiene que romper la operacion que lo origino.
    public interface IEnviadorCorreos
    {
        //Direccion de la app, para los links de los mails (por ejemplo "http://localhost:8085")
        String AppUrl { get; }

        void Enviar(String to, String asunto, String cuerpo);
    }
}

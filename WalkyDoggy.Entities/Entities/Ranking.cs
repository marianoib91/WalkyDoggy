using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities
{
    //Valoracion que un cliente le da a un paseador despues de un paseo: de 1 a 5 estrellas y un comentario opcional.
    //Hay una por reserva (BookingKey); WalkId es el primer paseo de esa reserva.
    public class Ranking : IEntityBase
    {
        public Int64 Id { get; set; }

        public Int64 WalkId { get; set; }

        public Int64 WalkerId { get; set; }

        public Int64 CustomerId { get; set; }

        public String BookingKey { get; set; }

        public DateTime Date { get; set; }

        //Estrellas, de 1 a 5
        public Double Score { get; set; }

        public String Comments { get; set; }

        //Un administrador la oculto (moderacion): no se muestra ni cuenta en los promedios, pero no se borra y se puede volver a mostrar
        public Boolean Hidden { get; set; }

        public String HiddenReason { get; set; }

        public DateTime? HiddenAt { get; set; }

        public Walk Walk { get; set; }

        public Walker Walker { get; set; }

        public Customer Customer { get; set; }
    }
}

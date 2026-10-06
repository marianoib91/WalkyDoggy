using System;
using System.Collections.Generic;

namespace WalkyDoggy.Application.Dtos
{
    /* ---------- Paseos (reservas) vistos por el administrador ---------- */

    //Una reserva (los paseos de un cliente con un paseador en un mismo dia y horario)
    public class AdminWalkDto
    {
        //Walk (paseo) | Stay (hospedaje de varias noches: Date es el ingreso, TimeFrom queda vacio)
        public String Kind { get; set; }

        public String BookingKey { get; set; }

        public DateTime Date { get; set; }

        public String TimeFrom { get; set; }

        //Solo hospedajes: dia de salida y noches
        public DateTime? CheckOut { get; set; }

        public Int32 Nights { get; set; }

        public String WalkerName { get; set; }

        public String CustomerName { get; set; }

        public String Pets { get; set; }

        public Int32 PetCount { get; set; }

        //Pending | Upcoming | InProgress | ToCollect | Collected | Cancelled (ver AdminWalkStatuses)
        public String Status { get; set; }

        //Walker | Customer | System | Admin (solo si esta cancelada)
        public String CancelledBy { get; set; }

        public String PaymentMethod { get; set; }

        public String PaymentStatus { get; set; }

        public Double Total { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }

        //Si todavia se puede cancelar de forma excepcional (no esta cancelada ni terminada)
        public Boolean CanCancel { get; set; }
    }

    public class AdminWalkPageDto
    {
        public Int32 Total { get; set; }

        public Int32 Page { get; set; }

        public Int32 PageSize { get; set; }

        public List<AdminWalkDto> Walks { get; set; }
    }

    public class CancelWalkDto
    {
        public String BookingKey { get; set; }

        //Por que se cancela (queda en la bitacora y se le informa a las dos partes)
        public String Reason { get; set; }
    }

    /* ---------- Tablero de metricas ---------- */

    public class DashboardCountDto
    {
        public String Key { get; set; }

        public Int32 Count { get; set; }
    }

    public class DashboardDayDto
    {
        //yyyy-MM-dd
        public String Date { get; set; }

        public Int32 Total { get; set; }

        public Int32 Cancelled { get; set; }
    }

    public class DashboardWalkerDto
    {
        public String Name { get; set; }

        public Int32 Bookings { get; set; }

        public Double? AverageRating { get; set; }
    }

    public class DashboardDto
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        //Reservas del periodo (por dia del paseo)
        public Int32 TotalBookings { get; set; }

        //Una entrada por estado (aunque sea 0), en el orden de AdminWalkStatuses
        public List<DashboardCountDto> ByStatus { get; set; }

        //Quien cancelo las reservas canceladas: Customer | Walker | System | Admin
        public List<DashboardCountDto> CancelledBy { get; set; }

        //Porcentaje de reservas canceladas sobre el total (0 a 100)
        public Double CancellationRate { get; set; }

        public Int32 ActiveWalkers { get; set; }

        public Int32 ActiveCustomers { get; set; }

        public Int32 PetsWalked { get; set; }

        //Hospedajes (ya incluidos en las reservas de arriba): cuantos no cancelados, cuantas noches y cuantos perros ya se hospedaron
        public Int32 StayBookings { get; set; }

        public Int32 StayNights { get; set; }

        public Int32 PetsBoarded { get; set; }

        //Plata de las reservas ya cobradas
        public Double CashCollected { get; set; }

        public Double MercadoPagoCollected { get; set; }

        public Double TotalCollected { get; set; }

        //Solo si la comision de Mercado Pago esta activa (porcentaje mayor a 0)
        public Decimal CommissionPercent { get; set; }

        public Double? EstimatedCommission { get; set; }

        //Valoraciones de paseadores hechas en el periodo (sin las ocultas)
        public Double? AverageRating { get; set; }

        public Int32 RatingCount { get; set; }

        public Int32 PetReviewCount { get; set; }

        //Denuncias: pendientes (nuevas y en revision, de siempre) y las que llegaron en el periodo
        public Int32 PendingComplaints { get; set; }

        public Int32 ComplaintsInPeriod { get; set; }

        //Datos generales del sistema (no dependen del periodo)
        public Int32 TotalCustomers { get; set; }

        public Int32 TotalWalkers { get; set; }

        public Int32 LockedUsers { get; set; }

        public List<DashboardDayDto> ByDay { get; set; }

        public List<DashboardWalkerDto> TopWalkers { get; set; }
    }
}

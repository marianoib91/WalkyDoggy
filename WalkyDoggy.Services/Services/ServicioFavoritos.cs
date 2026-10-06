using System;
using System.Collections.Generic;
using System.Linq;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;

namespace WalkyDoggy.Services.Services
{
    public class ServicioFavoritos : IServicioFavoritos
    {
        private readonly IRepositorioEntidadBase<FavoriteWalker> repositorioFavoritos;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioFavoritos(IRepositorioEntidadBase<FavoriteWalker> repositorioFavoritos,
                                 IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                 IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioFavoritos = repositorioFavoritos;
            this.repositorioPaseadores = repositorioPaseadores;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        public List<Int64> ObtenerIdsPaseadores(Int64 idCliente)
        {
            return this.repositorioFavoritos.BuscarPor(x => x.CustomerId == idCliente).
                                             Select(x => x.WalkerId).
                                             ToList();
        }

        public Boolean Establecer(SetFavoriteDto solicitud, out String error)
        {
            error = null;

            var existentes = this.repositorioFavoritos.BuscarPor(x => x.CustomerId == solicitud.CustomerId && x.WalkerId == solicitud.WalkerId).ToList();

            if (!solicitud.Favorite)
            {
                existentes.ForEach(x => this.repositorioFavoritos.Eliminar(x));
                this.unidadDeTrabajo.GuardarCambios();
                return true;
            }

            if (existentes.Count > 0)
            {
                return true;
            }

            if (!this.repositorioPaseadores.BuscarPor(x => x.Id == solicitud.WalkerId).Any())
            {
                error = "El paseador no existe.";
                return false;
            }

            this.repositorioFavoritos.Agregar(new FavoriteWalker
            {
                CustomerId = solicitud.CustomerId,
                WalkerId = solicitud.WalkerId,
                CreatedAt = DateTime.Now
            });
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }
    }
}

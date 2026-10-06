using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WalkyDoggy.Application.Constants;
using WalkyDoggy.Application.Dtos;
using WalkyDoggy.Data.Infrastructure;
using WalkyDoggy.Data.Repositories;
using WalkyDoggy.Entities;
using WalkyDoggy.Services.Contracts;
using WalkyDoggy.Services.Utilities;

namespace WalkyDoggy.Services.Services
{
    public class ServicioAdministracion : IServicioAdministracion
    {
        private const Int32 MinimoMotivo = 5;
        private const Int32 MaximoMotivo = 500;
        private const Int32 MaximoPagina = 50;

        private static readonly Regex FormatoEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly IRepositorioEntidadBase<User> repositorioUsuarios;
        private readonly IRepositorioEntidadBase<UserRole> repositorioRolesUsuario;
        private readonly IRepositorioEntidadBase<Customer> repositorioClientes;
        private readonly IRepositorioEntidadBase<Walker> repositorioPaseadores;
        private readonly IRepositorioEntidadBase<Walk> repositorioPaseos;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IServicioEncriptacion servicioEncriptacion;
        private readonly IServicioNotificaciones servicioNotificaciones;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioAdministracion(IRepositorioEntidadBase<User> repositorioUsuarios,
                                      IRepositorioEntidadBase<UserRole> repositorioRolesUsuario,
                                      IRepositorioEntidadBase<Customer> repositorioClientes,
                                      IRepositorioEntidadBase<Walker> repositorioPaseadores,
                                      IRepositorioEntidadBase<Walk> repositorioPaseos,
                                      IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                      IServicioEncriptacion servicioEncriptacion,
                                      IServicioNotificaciones servicioNotificaciones,
                                      IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioUsuarios = repositorioUsuarios;
            this.repositorioRolesUsuario = repositorioRolesUsuario;
            this.repositorioClientes = repositorioClientes;
            this.repositorioPaseadores = repositorioPaseadores;
            this.repositorioPaseos = repositorioPaseos;
            this.repositorioAcciones = repositorioAcciones;
            this.servicioEncriptacion = servicioEncriptacion;
            this.servicioNotificaciones = servicioNotificaciones;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Usuarios ---------- */

        public AdminUserPageDto ListarUsuarios(String rol, String estado, String buscar, Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            var usuarios = new List<AdminUserDto>();

            if (rol != "Walker")
            {
                usuarios.AddRange(this.repositorioClientes.ObtenerTodos().Select(c => new AdminUserDto
                {
                    UserId = c.UserId,
                    Role = "Customer",
                    ProfileId = c.Id,
                    FullName = c.FirstName + " " + c.LastName,
                    Email = c.User.Email,
                    Phone = c.Phone,
                    CreatedDate = c.User.CreatedDate,
                    IsLocked = c.User.IsLocked,
                    BlockReason = c.User.BlockReason,
                    BlockedAt = c.User.BlockedAt
                }).ToList());
            }

            if (rol != "Customer")
            {
                usuarios.AddRange(this.repositorioPaseadores.ObtenerTodos().Select(w => new AdminUserDto
                {
                    UserId = w.UserId,
                    Role = "Walker",
                    ProfileId = w.Id,
                    FullName = w.FirstName + " " + w.LastName,
                    Email = w.User.Email,
                    Phone = w.Phone,
                    CreatedDate = w.User.CreatedDate,
                    IsLocked = w.User.IsLocked,
                    BlockReason = w.User.BlockReason,
                    BlockedAt = w.User.BlockedAt
                }).ToList());
            }

            IEnumerable<AdminUserDto> filtrados = usuarios;

            if (estado == "Active")
            {
                filtrados = filtrados.Where(x => !x.IsLocked);
            }
            else if (estado == "Blocked")
            {
                filtrados = filtrados.Where(x => x.IsLocked);
            }

            if (!String.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                filtrados = filtrados.Where(x => Contiene(x.FullName, texto) || Contiene(x.Email, texto) || Contiene(x.Phone, texto));
            }

            var ordenados = filtrados.OrderBy(x => x.FullName, StringComparer.CurrentCultureIgnoreCase).ToList();
            var pagina_ = ordenados.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

            CompletarReservasProximas(pagina_);

            return new AdminUserPageDto { Total = ordenados.Count, Page = pagina, PageSize = tamanoPagina, Users = pagina_ };
        }

        private static Boolean Contiene(String texto, String buscado)
        {
            return texto != null && texto.IndexOf(buscado, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        //Cuantas reservas que todavia no empezaron tiene cada usuario de la pagina
        private void CompletarReservasProximas(List<AdminUserDto> usuarios)
        {
            if (usuarios.Count == 0)
            {
                return;
            }

            var idsPaseadores = usuarios.Where(x => x.Role == "Walker").Select(x => x.ProfileId).ToList();
            var idsClientes = usuarios.Where(x => x.Role == "Customer").Select(x => x.ProfileId).ToList();

            var paseos = ObtenerPaseosPorEmpezar(x => idsPaseadores.Contains(x.WalkerId) || idsClientes.Contains(x.Pet.CustomerId));

            foreach (var usuario in usuarios)
            {
                usuario.UpcomingBookings = paseos.
                    Where(x => usuario.Role == "Walker" ? x.WalkerId == usuario.ProfileId : x.Pet.CustomerId == usuario.ProfileId).
                    Select(AyudanteReservas.ClaveDe).
                    Distinct().
                    Count();
            }
        }

        //Paseos pendientes o confirmados que todavia no empezaron y cuyo horario todavia no llego
        private List<Walk> ObtenerPaseosPorEmpezar(System.Linq.Expressions.Expression<Func<Walk, Boolean>> condicion)
        {
            return this.repositorioPaseos.TodosConIncluidos(x => x.Pet).
                Where(condicion).
                Where(x => (x.Status == WalkStatus.Pending || x.Status == WalkStatus.Confirmed) && x.StartedAt == null && x.FinishedAt == null).
                ToList().
                Where(x => AyudanteReservas.InicioDe(x) > DateTime.Now).
                ToList();
        }

        /* ---------- Bloqueo ---------- */

        public Boolean Bloquear(Int64 idAdministrador, BlockUserDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null)
            {
                error = "Faltan datos.";
                return false;
            }

            var motivo = (solicitud.Reason ?? String.Empty).Trim();
            if (motivo.Length < MinimoMotivo || motivo.Length > MaximoMotivo)
            {
                error = "Escribí el motivo del bloqueo (entre " + MinimoMotivo + " y " + MaximoMotivo + " caracteres).";
                return false;
            }

            var usuario = this.repositorioUsuarios.TodosConIncluidos(x => x.UserRoles).FirstOrDefault(x => x.Id == solicitud.UserId);
            if (usuario == null)
            {
                error = "El usuario no existe.";
                return false;
            }
            if (usuario.Id == idAdministrador || usuario.UserRoles.Any(x => x.RoleId == Roles.Admin))
            {
                error = "No se puede bloquear a un administrador.";
                return false;
            }
            if (usuario.IsLocked)
            {
                error = "La cuenta ya está bloqueada.";
                return false;
            }

            var idPaseador = this.repositorioPaseadores.BuscarPor(x => x.UserId == usuario.Id).Select(x => (Int64?)x.Id).FirstOrDefault();
            var idCliente = this.repositorioClientes.BuscarPor(x => x.UserId == usuario.Id).Select(x => (Int64?)x.Id).FirstOrDefault();
            var rolBloqueado = idPaseador.HasValue ? "Walker" : "Customer";

            //Se cancelan las reservas que todavia no empezaron: el horario queda libre y la otra parte se entera por mail
            var paseosCancelados = ObtenerPaseosPorEmpezar(x => idPaseador.HasValue
                                                                ? x.WalkerId == idPaseador.Value
                                                                : (idCliente.HasValue && x.Pet.CustomerId == idCliente.Value));
            var ahora = DateTime.Now;
            foreach (var paseo in paseosCancelados)
            {
                paseo.Status = WalkStatus.Cancelled;
                paseo.Confirmed = false;
                paseo.CancelledBy = WalkCancelledBy.Admin;
                paseo.StatusChangedAt = ahora;
            }
            var reservasCanceladas = paseosCancelados.Select(AyudanteReservas.ClaveDe).Distinct().Count();

            usuario.IsLocked = true;
            usuario.BlockReason = motivo;
            usuario.BlockedAt = ahora;

            Registrar(idAdministrador, AdminActionTypes.BlockUser, usuario.Id,
                      "Motivo: " + motivo + " · Reservas canceladas: " + reservasCanceladas);
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.ReservaCanceladaPorBloqueo(paseosCancelados, rolBloqueado);
            this.servicioNotificaciones.CuentaBloqueada(usuario.Email, motivo);

            return true;
        }

        public Boolean Desbloquear(Int64 idAdministrador, UnblockUserDto solicitud, out String error)
        {
            error = null;

            var usuario = solicitud == null ? null : this.repositorioUsuarios.ObtenerUno(solicitud.UserId);
            if (usuario == null)
            {
                error = "El usuario no existe.";
                return false;
            }
            if (!usuario.IsLocked)
            {
                error = "La cuenta no está bloqueada.";
                return false;
            }

            usuario.IsLocked = false;
            usuario.BlockReason = null;
            usuario.BlockedAt = null;

            Registrar(idAdministrador, AdminActionTypes.UnblockUser, usuario.Id, null);
            this.unidadDeTrabajo.GuardarCambios();

            this.servicioNotificaciones.CuentaDesbloqueada(usuario.Email);

            return true;
        }

        /* ---------- Administradores ---------- */

        public List<AdminAccountDto> ListarAdministradores()
        {
            return this.repositorioUsuarios.TodosConIncluidos(x => x.UserRoles).
                Where(x => x.UserRoles.Any(r => r.RoleId == Roles.Admin)).
                OrderBy(x => x.Email).
                ToList().
                Select(x => new AdminAccountDto { UserId = x.Id, Email = x.Email, CreatedDate = x.CreatedDate, IsLocked = x.IsLocked }).
                ToList();
        }

        public Boolean CrearAdministrador(Int64 idAdministrador, NewAdminDto solicitud, out String error)
        {
            error = null;

            var email = solicitud == null ? null : (solicitud.Email ?? String.Empty).Trim();
            if (String.IsNullOrEmpty(email) || email.Length > 200 || !FormatoEmail.IsMatch(email))
            {
                error = "Ingresá un mail válido.";
                return false;
            }
            if (solicitud.Password == null || solicitud.Password.Length < 6 || solicitud.Password.Length > 50)
            {
                error = "La contraseña debe tener entre 6 y 50 caracteres.";
                return false;
            }
            if (this.repositorioUsuarios.BuscarPor(x => x.Email == email).Any())
            {
                error = "Ya existe un usuario con ese mail.";
                return false;
            }

            var sal = this.servicioEncriptacion.CrearSal();
            var usuario = new User
            {
                Email = email,
                Salt = sal,
                HashedPassword = this.servicioEncriptacion.EncriptarContrasena(solicitud.Password, sal),
                IsLocked = false,
                CreatedDate = DateTime.Now
            };
            this.repositorioUsuarios.Agregar(usuario);
            this.unidadDeTrabajo.GuardarCambios();

            this.repositorioRolesUsuario.Agregar(new UserRole { UserId = usuario.Id, RoleId = Roles.Admin });
            Registrar(idAdministrador, AdminActionTypes.CreateAdmin, usuario.Id, null);
            this.unidadDeTrabajo.GuardarCambios();

            return true;
        }

        /* ---------- Bitacora ---------- */

        public AdminActionPageDto ListarBitacora(Int32 pagina, Int32 tamanoPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Min(MaximoPagina, Math.Max(1, tamanoPagina));

            var consulta = this.repositorioAcciones.ObtenerTodos();
            var total = consulta.Count();
            var acciones = consulta.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).
                Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

            var idsUsuarios = acciones.Select(x => x.AdminUserId).
                Concat(acciones.Where(x => x.TargetUserId.HasValue).Select(x => x.TargetUserId.Value)).
                Distinct().ToList();
            var emails = this.repositorioUsuarios.BuscarPor(x => idsUsuarios.Contains(x.Id)).
                Select(x => new { x.Id, x.Email }).ToList().
                ToDictionary(x => x.Id, x => x.Email);

            return new AdminActionPageDto
            {
                Total = total,
                Page = pagina,
                PageSize = tamanoPagina,
                Actions = acciones.Select(x => new AdminActionDto
                {
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    AdminEmail = emails.ContainsKey(x.AdminUserId) ? emails[x.AdminUserId] : null,
                    Action = x.Action,
                    TargetEmail = x.TargetUserId.HasValue && emails.ContainsKey(x.TargetUserId.Value) ? emails[x.TargetUserId.Value] : null,
                    Detail = x.Detail
                }).ToList()
            };
        }

        //Queda guardada junto con el resto de los cambios de la operacion
        private void Registrar(Int64 idAdministrador, String accion, Int64? idUsuarioAfectado, String detalle)
        {
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = accion,
                TargetUserId = idUsuarioAfectado,
                Detail = detalle,
                CreatedAt = DateTime.Now
            });
        }
    }
}

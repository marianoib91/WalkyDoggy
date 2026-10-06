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
    public class ServicioPublicidad : IServicioPublicidad
    {
        //Carpeta publica (dentro del sitio) donde estan las imagenes de los avisos
        public const String RutaImagenes = "/Content/images/uploads/ads/";

        private const Int32 MinimoNombre = 2;
        private const Int32 MaximoNombre = 80;
        private const Int32 MinimoTitulo = 3;
        private const Int32 MaximoTitulo = 60;
        private const Int32 MaximoTextoAviso = 200;
        private const Int32 MaximoDescripcion = 300;
        private const Int32 MaximoRadioKm = 50;
        private const Int32 MaximoDiasDeUnAviso = 366;
        private const Int32 MaximoAvisosPorPedido = 5;
        private const Int32 MaximoPromocionesPorComercio = 3;
        private const Int32 MaximoIdsPorPedido = 10;

        private static readonly Regex FormatoTelefono = new Regex(@"^[0-9 +\-()]{6,30}$", RegexOptions.Compiled);

        private readonly IRepositorioEntidadBase<Advertiser> repositorioComercios;
        private readonly IRepositorioEntidadBase<Ad> repositorioAvisos;
        private readonly IRepositorioEntidadBase<AdminAction> repositorioAcciones;
        private readonly IUnidadDeTrabajo unidadDeTrabajo;

        public ServicioPublicidad(IRepositorioEntidadBase<Advertiser> repositorioComercios,
                                  IRepositorioEntidadBase<Ad> repositorioAvisos,
                                  IRepositorioEntidadBase<AdminAction> repositorioAcciones,
                                  IUnidadDeTrabajo unidadDeTrabajo)
        {
            this.repositorioComercios = repositorioComercios;
            this.repositorioAvisos = repositorioAvisos;
            this.repositorioAcciones = repositorioAcciones;
            this.unidadDeTrabajo = unidadDeTrabajo;
        }

        /* ---------- Comercios ---------- */

        public List<AdvertiserDto> ListarComercios()
        {
            var hoy = DateTime.Now.Date;
            var avisos = this.repositorioAvisos.ObtenerTodos().Select(x => new { x.AdvertiserId, x.Active, x.StartDate, x.EndDate }).ToList();

            return this.repositorioComercios.ObtenerTodos().ToList().
                OrderByDescending(x => x.Active).
                ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).
                Select(x => new AdvertiserDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Category = x.Category,
                    Description = x.Description,
                    Phone = x.Phone,
                    Website = x.Website,
                    StreetName = x.StreetName,
                    StreetNumber = x.StreetNumber,
                    CityName = x.CityName,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Active = x.Active,
                    AdCount = avisos.Count(a => a.AdvertiserId == x.Id),
                    RunningAdCount = x.Active ? avisos.Count(a => a.AdvertiserId == x.Id && a.Active && a.StartDate <= hoy && a.EndDate >= hoy) : 0
                }).
                ToList();
        }

        public Boolean GuardarComercio(Int64 idAdministrador, SaveAdvertiserDto solicitud, out String error)
        {
            error = null;

            if (solicitud == null)
            {
                error = "Faltan datos.";
                return false;
            }

            var nombre = Normalizar(solicitud.Name);
            if (nombre.Length < MinimoNombre || nombre.Length > MaximoNombre)
            {
                error = "El nombre del comercio tiene que tener entre " + MinimoNombre + " y " + MaximoNombre + " caracteres.";
                return false;
            }
            if (!AdvertiserCategories.Todos.Contains(solicitud.Category ?? String.Empty))
            {
                error = "Elegí el rubro del comercio.";
                return false;
            }

            var descripcion = Normalizar(solicitud.Description);
            if (descripcion.Length > MaximoDescripcion)
            {
                error = "La descripción puede tener hasta " + MaximoDescripcion + " caracteres.";
                return false;
            }

            var telefono = (solicitud.Phone ?? String.Empty).Trim();
            if (telefono.Length > 0 && !FormatoTelefono.IsMatch(telefono))
            {
                error = "El teléfono solo puede tener números, espacios y los signos + - ( ).";
                return false;
            }

            var web = (solicitud.Website ?? String.Empty).Trim();
            if (web.Length > 0 && !EsEnlaceValido(web))
            {
                error = "La página web tiene que empezar con http:// o https:// (máximo 200 caracteres).";
                return false;
            }

            var calle = Normalizar(solicitud.StreetName);
            var ciudad = Normalizar(solicitud.CityName);
            if (calle.Length > 100 || ciudad.Length > 100)
            {
                error = "La dirección es demasiado larga.";
                return false;
            }

            var latitud = (solicitud.Latitude ?? String.Empty).Trim();
            var longitud = (solicitud.Longitude ?? String.Empty).Trim();
            if (latitud.Length > 0 || longitud.Length > 0)
            {
                Double latitudLeida, longitudLeida;
                if (!Geografia.IntentarLeerCoordenadas(latitud, longitud, out latitudLeida, out longitudLeida))
                {
                    error = "La ubicación del comercio no es válida.";
                    return false;
                }
            }

            var existentes = this.repositorioComercios.ObtenerTodos().ToList();
            if (existentes.Any(x => x.Id != solicitud.Id && String.Equals(x.Name, nombre, StringComparison.CurrentCultureIgnoreCase)))
            {
                error = "Ya existe un comercio con ese nombre.";
                return false;
            }

            Advertiser comercio;
            String detalle;
            if (solicitud.Id == 0)
            {
                comercio = new Advertiser { Active = true, CreatedAt = DateTime.Now };
                this.repositorioComercios.Agregar(comercio);
                detalle = "Comercio creado: " + nombre;
            }
            else
            {
                comercio = existentes.FirstOrDefault(x => x.Id == solicitud.Id);
                if (comercio == null)
                {
                    error = "El comercio no existe.";
                    return false;
                }
                detalle = "Comercio modificado: " + nombre;
            }

            comercio.Name = nombre;
            comercio.Category = solicitud.Category;
            comercio.Description = descripcion.Length == 0 ? null : descripcion;
            comercio.Phone = telefono.Length == 0 ? null : telefono;
            comercio.Website = web.Length == 0 ? null : web;
            comercio.StreetName = calle.Length == 0 ? null : calle;
            comercio.StreetNumber = solicitud.StreetNumber.HasValue && solicitud.StreetNumber.Value > 0 ? solicitud.StreetNumber : null;
            comercio.CityName = ciudad.Length == 0 ? null : ciudad;
            comercio.Latitude = latitud.Length == 0 ? null : latitud;
            comercio.Longitude = longitud.Length == 0 ? null : longitud;

            Registrar(idAdministrador, AdminActionTypes.SaveAdvertiser, detalle);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean EstablecerComercioActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error)
        {
            error = null;

            var comercio = solicitud == null ? null : this.repositorioComercios.ObtenerUno(solicitud.Id);
            if (comercio == null)
            {
                error = "El comercio no existe.";
                return false;
            }
            if (comercio.Active == solicitud.Active)
            {
                return true;
            }

            comercio.Active = solicitud.Active;
            Registrar(idAdministrador, AdminActionTypes.SetAdvertiserActive,
                      "Comercio " + (solicitud.Active ? "reactivado: " : "dado de baja: ") + comercio.Name);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Avisos ---------- */

        public List<AdminAdDto> ListarAvisos()
        {
            var hoy = DateTime.Now.Date;

            return this.repositorioAvisos.TodosConIncluidos(x => x.Advertiser).ToList().
                OrderByDescending(x => x.Active && x.Advertiser.Active && x.StartDate <= hoy && x.EndDate >= hoy).
                ThenByDescending(x => x.EndDate).
                ThenByDescending(x => x.Id).
                Select(x => new AdminAdDto
                {
                    Id = x.Id,
                    AdvertiserId = x.AdvertiserId,
                    AdvertiserName = x.Advertiser.Name,
                    AdvertiserCategory = x.Advertiser.Category,
                    Title = x.Title,
                    Text = x.Text,
                    ImageUrl = UrlDeImagen(x.ImageFile),
                    LinkUrl = x.LinkUrl,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Audience = x.Audience,
                    RadiusKm = x.RadiusKm,
                    Active = x.Active,
                    Status = EstadoDe(x, hoy),
                    Impressions = x.Impressions,
                    Clicks = x.Clicks,
                    ClickRate = x.Impressions == 0 ? 0 : Math.Round(100.0 * x.Clicks / x.Impressions, 1)
                }).
                ToList();
        }

        public Boolean GuardarAviso(Int64 idAdministrador, SaveAdDto solicitud, out Int64 idAviso, out String error)
        {
            error = null;
            idAviso = 0;

            if (solicitud == null)
            {
                error = "Faltan datos.";
                return false;
            }

            var comercio = this.repositorioComercios.ObtenerUno(solicitud.AdvertiserId);
            if (comercio == null)
            {
                error = "Elegí el comercio del aviso.";
                return false;
            }

            var titulo = Normalizar(solicitud.Title);
            if (titulo.Length < MinimoTitulo || titulo.Length > MaximoTitulo)
            {
                error = "El título tiene que tener entre " + MinimoTitulo + " y " + MaximoTitulo + " caracteres.";
                return false;
            }

            var texto = Normalizar(solicitud.Text);
            if (texto.Length > MaximoTextoAviso)
            {
                error = "El texto puede tener hasta " + MaximoTextoAviso + " caracteres.";
                return false;
            }

            var enlace = (solicitud.LinkUrl ?? String.Empty).Trim();
            if (enlace.Length > 0 && !EsEnlaceValido(enlace))
            {
                error = "El enlace tiene que empezar con http:// o https:// (máximo 200 caracteres).";
                return false;
            }

            var desde = solicitud.StartDate.Date;
            var hasta = solicitud.EndDate.Date;
            if (desde.Year < 2020 || hasta.Year < 2020)
            {
                error = "Elegí las fechas en que se muestra el aviso.";
                return false;
            }
            if (hasta < desde)
            {
                error = "La fecha de fin no puede ser anterior a la de inicio.";
                return false;
            }
            if ((hasta - desde).TotalDays + 1 > MaximoDiasDeUnAviso)
            {
                error = "Un aviso puede durar como máximo " + MaximoDiasDeUnAviso + " días.";
                return false;
            }

            if (!AdAudiences.Todos.Contains(solicitud.Audience ?? String.Empty))
            {
                error = "Elegí a quién se le muestra el aviso.";
                return false;
            }

            Int32? radio = solicitud.RadiusKm.HasValue && solicitud.RadiusKm.Value > 0 ? solicitud.RadiusKm : null;
            if (radio.HasValue)
            {
                if (radio.Value > MaximoRadioKm)
                {
                    error = "El alcance puede ser de hasta " + MaximoRadioKm + " km.";
                    return false;
                }
                Double latitud, longitud;
                if (!Geografia.IntentarLeerCoordenadas(comercio.Latitude, comercio.Longitude, out latitud, out longitud))
                {
                    error = "El comercio no tiene ubicación en el mapa: cargá su dirección para poder limitar el aviso por zona.";
                    return false;
                }
            }

            Ad aviso;
            String detalle;
            if (solicitud.Id == 0)
            {
                if (!comercio.Active)
                {
                    error = "El comercio está dado de baja: reactivalo para crearle avisos.";
                    return false;
                }
                if (hasta < DateTime.Now.Date)
                {
                    error = "El aviso terminaría antes de hoy: elegí una fecha de fin que no haya pasado.";
                    return false;
                }

                aviso = new Ad { Active = true, CreatedAt = DateTime.Now };
                this.repositorioAvisos.Agregar(aviso);
                detalle = "Aviso creado: " + titulo + " (" + comercio.Name + ")";
            }
            else
            {
                aviso = this.repositorioAvisos.ObtenerUno(solicitud.Id);
                if (aviso == null)
                {
                    error = "El aviso no existe.";
                    return false;
                }
                detalle = "Aviso modificado: " + titulo + " (" + comercio.Name + ")";
            }

            aviso.AdvertiserId = comercio.Id;
            aviso.Title = titulo;
            aviso.Text = texto.Length == 0 ? null : texto;
            aviso.LinkUrl = enlace.Length == 0 ? null : enlace;
            aviso.StartDate = desde;
            aviso.EndDate = hasta;
            aviso.Audience = solicitud.Audience;
            aviso.RadiusKm = radio;

            Registrar(idAdministrador, AdminActionTypes.SaveAd, detalle);
            this.unidadDeTrabajo.GuardarCambios();

            idAviso = aviso.Id;
            return true;
        }

        public Boolean EstablecerAvisoActivo(Int64 idAdministrador, SetCatalogItemActiveDto solicitud, out String error)
        {
            error = null;

            var aviso = solicitud == null ? null : this.repositorioAvisos.ObtenerUno(solicitud.Id);
            if (aviso == null)
            {
                error = "El aviso no existe.";
                return false;
            }
            if (aviso.Active == solicitud.Active)
            {
                return true;
            }

            aviso.Active = solicitud.Active;
            Registrar(idAdministrador, AdminActionTypes.SetAdActive, "Aviso " + (solicitud.Active ? "reanudado: " : "pausado: ") + aviso.Title);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        public Boolean CambiarImagen(Int64 idAdministrador, Int64 idAviso, String archivoNuevo, out String archivoAnterior, out String error)
        {
            error = null;
            archivoAnterior = null;

            var aviso = this.repositorioAvisos.ObtenerUno(idAviso);
            if (aviso == null)
            {
                error = "El aviso no existe.";
                return false;
            }

            archivoAnterior = aviso.ImageFile;
            aviso.ImageFile = String.IsNullOrEmpty(archivoNuevo) ? null : archivoNuevo;
            Registrar(idAdministrador, AdminActionTypes.SetAdImage, (archivoNuevo == null ? "Imagen quitada del aviso: " : "Imagen cambiada del aviso: ") + aviso.Title);
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Lo que ve quien usa la app ---------- */

        public List<PublicAdDto> ObtenerActivos(String audiencia, Double? latitud, Double? longitud, Int32 maximo)
        {
            maximo = Math.Min(MaximoAvisosPorPedido, Math.Max(1, maximo));
            var elegibles = AvisosParaMostrar(audiencia, latitud, longitud);

            //Se reparte la exposicion: primero los que menos se mostraron y, a igual cantidad, en orden variable
            var azar = new Random();
            return elegibles.
                OrderBy(x => x.Key.Impressions).
                ThenBy(x => azar.Next()).
                Take(maximo).
                Select(x => new PublicAdDto
                {
                    Id = x.Key.Id,
                    Title = x.Key.Title,
                    Text = x.Key.Text,
                    ImageUrl = UrlDeImagen(x.Key.ImageFile),
                    LinkUrl = x.Key.LinkUrl,
                    AdvertiserName = x.Key.Advertiser.Name,
                    Category = x.Key.Advertiser.Category,
                    Address = DireccionDe(x.Key.Advertiser),
                    Phone = x.Key.Advertiser.Phone,
                    DistanceKm = x.Value
                }).
                ToList();
        }

        public List<PublicAdvertiserDto> ObtenerComerciosAmigos(String audiencia, Double? latitud, Double? longitud)
        {
            var promociones = AvisosParaMostrar(audiencia, latitud, longitud).
                GroupBy(x => x.Key.AdvertiserId).
                ToDictionary(x => x.Key, x => x.Select(a => a.Key).OrderBy(a => a.EndDate).ThenBy(a => a.Id).Take(MaximoPromocionesPorComercio).ToList());

            return this.repositorioComercios.BuscarPor(x => x.Active).ToList().
                Select(comercio =>
                {
                    Double latitudComercio, longitudComercio;
                    var ubicado = Geografia.IntentarLeerCoordenadas(comercio.Latitude, comercio.Longitude, out latitudComercio, out longitudComercio);
                    Double? distancia = ubicado && latitud.HasValue && longitud.HasValue
                        ? Math.Round(Geografia.DistanciaEnKilometros(latitud.Value, longitud.Value, latitudComercio, longitudComercio), 1)
                        : (Double?)null;

                    return new PublicAdvertiserDto
                    {
                        Id = comercio.Id,
                        Name = comercio.Name,
                        Category = comercio.Category,
                        Description = comercio.Description,
                        Phone = comercio.Phone,
                        Website = comercio.Website,
                        Address = DireccionDe(comercio),
                        Latitude = ubicado ? comercio.Latitude : null,
                        Longitude = ubicado ? comercio.Longitude : null,
                        DistanceKm = distancia,
                        Promotions = (promociones.ContainsKey(comercio.Id) ? promociones[comercio.Id] : new List<Ad>()).
                            Select(a => new PublicPromotionDto { AdId = a.Id, Title = a.Title, Text = a.Text, LinkUrl = a.LinkUrl }).ToList()
                    };
                }).
                //Los mas cercanos primero (los que no se pueden ubicar, al final) y, a igual distancia, por nombre
                OrderBy(x => x.DistanceKm.HasValue ? 0 : 1).
                ThenBy(x => x.DistanceKm ?? 0).
                ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).
                ToList();
        }

        //Los avisos que hoy corresponde mostrar a esa audiencia y ubicacion, con la distancia al comercio de cada uno:
        //activos (el aviso y su comercio), dentro de sus fechas y, si tienen alcance por zona, solo si quien mira esta dentro del radio
        private List<KeyValuePair<Ad, Double?>> AvisosParaMostrar(String audiencia, Double? latitud, Double? longitud)
        {
            var hoy = DateTime.Now.Date;
            var paraEstaAudiencia = audiencia == AdAudiences.Walkers ? AdAudiences.Walkers : AdAudiences.Customers;

            var candidatos = this.repositorioAvisos.TodosConIncluidos(x => x.Advertiser).
                Where(x => x.Active && x.Advertiser.Active && x.StartDate <= hoy && x.EndDate >= hoy &&
                           (x.Audience == AdAudiences.All || x.Audience == paraEstaAudiencia)).
                ToList();

            var elegibles = new List<KeyValuePair<Ad, Double?>>();
            foreach (var aviso in candidatos)
            {
                Double latitudComercio, longitudComercio;
                var comercioUbicado = Geografia.IntentarLeerCoordenadas(aviso.Advertiser.Latitude, aviso.Advertiser.Longitude, out latitudComercio, out longitudComercio);
                Double? distancia = comercioUbicado && latitud.HasValue && longitud.HasValue
                    ? Math.Round(Geografia.DistanciaEnKilometros(latitud.Value, longitud.Value, latitudComercio, longitudComercio), 1)
                    : (Double?)null;

                //Un aviso con alcance por zona solo se muestra a quien se sabe que esta dentro del radio
                if (aviso.RadiusKm.HasValue && (!distancia.HasValue || distancia.Value > aviso.RadiusKm.Value))
                {
                    continue;
                }

                elegibles.Add(new KeyValuePair<Ad, Double?>(aviso, distancia));
            }

            return elegibles;
        }

        public void RegistrarVistas(IEnumerable<Int64> idsAvisos)
        {
            var ids = (idsAvisos ?? new List<Int64>()).Distinct().Take(MaximoIdsPorPedido).ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var hoy = DateTime.Now.Date;
            foreach (var aviso in this.repositorioAvisos.BuscarPor(x => ids.Contains(x.Id) && x.Active && x.StartDate <= hoy && x.EndDate >= hoy).ToList())
            {
                aviso.Impressions++;
            }
            this.unidadDeTrabajo.GuardarCambios();
        }

        public Boolean RegistrarClic(Int64 idAviso)
        {
            var hoy = DateTime.Now.Date;
            var aviso = this.repositorioAvisos.BuscarPor(x => x.Id == idAviso && x.Active && x.StartDate <= hoy && x.EndDate >= hoy).FirstOrDefault();
            if (aviso == null)
            {
                return false;
            }

            aviso.Clicks++;
            this.unidadDeTrabajo.GuardarCambios();
            return true;
        }

        /* ---------- Auxiliares ---------- */

        private static String EstadoDe(Ad aviso, DateTime hoy)
        {
            if (!aviso.Active || !aviso.Advertiser.Active)
            {
                return "Paused";
            }
            if (aviso.EndDate < hoy)
            {
                return "Finished";
            }
            return aviso.StartDate > hoy ? "Scheduled" : "Running";
        }

        private static String UrlDeImagen(String archivo)
        {
            return String.IsNullOrEmpty(archivo) ? null : RutaImagenes + archivo;
        }

        private static String DireccionDe(Advertiser comercio)
        {
            var calle = String.Join(" ", new[] { comercio.StreetName, comercio.StreetNumber.HasValue ? comercio.StreetNumber.Value.ToString() : null }.Where(x => !String.IsNullOrWhiteSpace(x)));
            var direccion = String.Join(", ", new[] { calle, comercio.CityName }.Where(x => !String.IsNullOrWhiteSpace(x)));
            return direccion.Length == 0 ? null : direccion;
        }

        //Solo http o https: nunca "javascript:" ni otros esquemas
        private static Boolean EsEnlaceValido(String enlace)
        {
            Uri uri;
            return enlace.Length <= 200 &&
                   Uri.TryCreate(enlace, UriKind.Absolute, out uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                   !String.IsNullOrEmpty(uri.Host);
        }

        //Sin espacios de mas (al principio, al final ni repetidos)
        private static String Normalizar(String texto)
        {
            return Regex.Replace((texto ?? String.Empty).Trim(), @"\s+", " ");
        }

        private void Registrar(Int64 idAdministrador, String accion, String detalle)
        {
            this.repositorioAcciones.Agregar(new AdminAction
            {
                AdminUserId = idAdministrador,
                Action = accion,
                Detail = detalle.Length > 1000 ? detalle.Substring(0, 1000) : detalle,
                CreatedAt = DateTime.Now
            });
        }
    }
}

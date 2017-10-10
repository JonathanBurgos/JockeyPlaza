using System.Linq;
using static jockeyPlaza.Models.Database.ConexionDatabase;
using jockeyPlaza.Models.Database;
using System;
using System.Collections.Generic;
using jockeyPlaza.Models.ViewModel;
using jockeyPlaza.Enumerables.Modulos;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace jockeyPlaza.Respositories
{
    public class Repositorio
    {
        private readonly DatabaseContext ConexionDatabase;
        private string hostUrl;
        
        // Constructor
        public Repositorio(DatabaseContext databaseContext, IConfiguration configuration)
        {
            ConexionDatabase = databaseContext;
            hostUrl = configuration["Imagenes:RutaWeb"];
        }

        // Obtener la ruta de las imagenes
        public string getUrlImagenes()
        {
            return hostUrl;
        }

        // Función para obtener una imagen según su id
        public ImagenViewModel obtenerImagen(long? id)
        {
            // Obtener de base de datos
            Imagen imagenBD = ConexionDatabase.Imagenes.Where(x => x.Id == id).FirstOrDefault();
            if (imagenBD == null) return null;

            // Armar el view model
            ImagenViewModel imagen = new ImagenViewModel
            {
                Id = imagenBD.Id,
                Icono = hostUrl + imagenBD.Url
            };

            // Retorno
            return imagen;
        }

        // funcion para onbtener rango de color dependiendo de la zona.
        public string obtenerColorRango(int porcentaje, int zonaid)
        {
            // Consultar los ragon de la zona
            var rangoZonaBD = ConexionDatabase.Zonas.Where(x => x.Id == zonaid).FirstOrDefault();

            // Validacion de porcentaje para determinar color de la zona.
            if (porcentaje <= rangoZonaBD.Rojo)
                return "#FF0000";
            else if (porcentaje > rangoZonaBD.Rojo && porcentaje <= rangoZonaBD.Amarillo)
                return "#FFBF00";
            else
                return "#04B45F";
        }

        // Función para obtener la lista de imagenes del banner para un elemento
        public List<ImagenBannerViewModel> obtenerImagenesBanner(int idmodulo, int id)
        {
            // Listado de imagenes a retornar
            List<ImagenBannerViewModel> imagenesBanner = new List<ImagenBannerViewModel>();

            // Obtener de base de datos el elemento
            Elemento elementoBD = ConexionDatabase.Elementos.Where(x => x.ModuloId == idmodulo && x.NumeroElemento == id).FirstOrDefault();
            if (elementoBD == null) return imagenesBanner;

            // Obtener las imagenes asociadas al elemento
            List<ImagenElemento> imagenesElementoBD = ConexionDatabase.ImagenesElemento.Where(x => x.ElementoId == elementoBD.Id).OrderBy(y=> y.Orden).ToList();
            foreach(ImagenElemento imagenElementoBD in imagenesElementoBD)
            {
                // Obtener la imagen
                ImagenViewModel imagen = obtenerImagen(imagenElementoBD.ImagenId);
                imagenesBanner.Add(new ImagenBannerViewModel
                {
                    Id = imagen.Id,
                    Imagen = imagen.Icono,
                    Segundos = imagenElementoBD.Duracion,
                    Orden = imagenElementoBD.Orden
                });
            }

            // Retorno
            return imagenesBanner;
        }

        // Función para obtener una ubicación según su id
        public UbicacionViewModel obtenerUbicacion(int? id)
        {
            // Si viene NUll
            if (id == null) return null;

            // Obtener de base de datos
            var ubicacionBD = (from u in ConexionDatabase.Ubicaciones
                               join z in ConexionDatabase.Zonas on u.ZonaId equals z.Id
                               join n in ConexionDatabase.Niveles on u.NivelId equals n.Id
                               where u.Id == id
                               select new { u.Id, u.ZonaId, u.NivelId, u.SectorId, NombreZona = z.Nombre, NombreNivel = n.Nombre }).FirstOrDefault();

            // Obtener la información de nivel zona (si se posee)
            NivelZona nivelZonaBD = ConexionDatabase.NivelesZonas.Where(x => x.IdZona == ubicacionBD.ZonaId && x.IdNivel == ubicacionBD.NivelId).FirstOrDefault();

            // Armar el view model
            UbicacionViewModel ubicacion = new UbicacionViewModel
            {
                Id = ubicacionBD.Id,
                IdZona = ubicacionBD.ZonaId,
                IdNivel = ubicacionBD.NivelId,
                IdSector = ubicacionBD.SectorId,
                NombreZona = ubicacionBD.NombreZona,
                NombreNivel = (nivelZonaBD != null && nivelZonaBD.AliasNivel != null) ? nivelZonaBD.AliasNivel: ubicacionBD.NombreNivel
            };

            // Retorno
            return ubicacion;
        }

        // Función para obtener las ubicaciones de un elemento
        public List<UbicacionViewModel> obtenerUbicacionesElemento(int idmodulo, int id)
        {
            // Listado de ubicaciones a retornar
            List<UbicacionViewModel> ubicaciones = new List<UbicacionViewModel>();

            // Obtener las ubicaciones asociadas al elemento
            var ubicacionElementoBD = (from e in ConexionDatabase.Elementos
                                       join u in ConexionDatabase.UbicacionesElemento on e.Id equals u.ElementoId
                                       where e.ModuloId == idmodulo && e.NumeroElemento == id
                                       select new { u.UbicacionId }).ToList(); 
            // Validar Consulta.
            if (ubicacionElementoBD == null) return ubicaciones;
            ubicacionElementoBD.ForEach(x =>
            {
                // Obtener la ubicación
                UbicacionViewModel ubicacion = obtenerUbicacion(x.UbicacionId);
                ubicaciones.Add(ubicacion);
            });           

            // Retorno
            return ubicaciones;
        }

        // Guardar Contratos en base de datos
        public void guardarContratos(List<Contrato> contratos)
        {
            // Validar que los contratos no vengan nulos.
            if (contratos != null)
            {    
                // Recorrer todos los contratos
                contratos.ForEach(x => 
                {
                    // Consultar en la base de datos el contratod
                    var contratosBD = ConexionDatabase.Contratos.Where(y => y.CodigoInternoContrato == x.CodigoInternoContrato).FirstOrDefault();

                    // Validar que exista el contrato
                    if(contratosBD == null)
                    {
                        // Agregar un nuevo contrato
                        ConexionDatabase.Contratos.Add(new Contrato
                        {
                            FechaSalida = x.FechaSalida,
                            NombreComercial = x.NombreComercial,
                            NumeroLocal = x.NumeroLocal,
                            NumeroContrato = x.NumeroContrato,
                            RazonSocial = x.RazonSocial,
                            CodigoInternoContrato = x.CodigoInternoContrato,
                            CodigoInternoLocal = x.CodigoInternoLocal,
                            FechaEntrada = x.FechaEntrada,
                            CodigoInternoSector = x.CodigoInternoSector,
                            DescripcionSector = x.DescripcionSector,
                            CodigoInternoRubro = x.CodigoInternoRubro,
                            Estado = x.Estado,
                            FechaDesde = x.FechaDesde,
                            FechaHasta = x.FechaHasta,
                            TipoLocal = x.TipoLocal,
                            Ventas = x.Ventas,
                            Transaccion = x.Transaccion,
                            AuditInsertUser = 1,
                            AuditInsertDate = DateTime.Now,
                            AuditUpdateUser = 1,
                            AuditUpdateDate = DateTime.Now,
                        });
                    }
                    else
                    {
                        // Actualizar el contratos
                        contratosBD.FechaSalida = x.FechaSalida;
                        contratosBD.NombreComercial = x.NombreComercial;
                        contratosBD.NumeroLocal = x.NumeroLocal;
                        contratosBD.NumeroContrato = x.NumeroContrato;
                        contratosBD.RazonSocial = x.RazonSocial;
                        contratosBD.CodigoInternoContrato = x.CodigoInternoContrato;
                        contratosBD.CodigoInternoLocal = x.CodigoInternoLocal;
                        contratosBD.FechaEntrada = x.FechaEntrada;
                        contratosBD.CodigoInternoSector = x.CodigoInternoSector;
                        contratosBD.DescripcionSector = x.DescripcionSector;
                        contratosBD.CodigoInternoRubro = x.CodigoInternoRubro;
                        contratosBD.Estado = x.Estado;
                        contratosBD.FechaDesde = x.FechaDesde;
                        contratosBD.FechaHasta = x.FechaHasta;
                        contratosBD.TipoLocal = x.TipoLocal;
                        contratosBD.Ventas = x.Ventas;
                        contratosBD.Transaccion = x.Transaccion;
                        contratosBD.AuditUpdateUser = 1;
                        contratosBD.AuditUpdateDate = DateTime.Now;
                    }
                    ConexionDatabase.SaveChanges();
                });               
            }
        }

        public TiendaViewModel obtenerTienda(int? idTienda)
        {
            // Intanciar tienda View Model
            TiendaViewModel tienda = new TiendaViewModel();

            // Obtener tienda de la base de datos
            Tienda tiendaBD = ConexionDatabase.Tiendas.Where(x => x.Id == idTienda).FirstOrDefault();
            if (tiendaBD == null) return null;
            
            // Datos de la tienda
            var imagenTienda = obtenerImagen(tiendaBD.ImagenId);
            var infoCategoria = ConexionDatabase.Categorias.Where(x => x.Id == tiendaBD.CategoriaId).FirstOrDefault();
            var imagenCategoria = obtenerImagen(infoCategoria.ImagenId);
            var imagenesBanner = obtenerImagenesBanner((int)IModulo.Tiendas, tiendaBD.Id);
            var ubicacionTienda = obtenerUbicacionesElemento((int)IModulo.Tiendas, tiendaBD.Id);
            var etiquetasTienda = obtenerEtiquetasElemento((int)IModulo.Tiendas, tiendaBD.Id);

            // Contrato
            Contrato contrato = ConexionDatabase.Contratos.Where(x => x.Id == tiendaBD.Contrato).FirstOrDefault();
            ContratosViewModel contratoView = new ContratosViewModel();
            if (contrato != null)
            {
                contratoView = new ContratosViewModel
                {
                    Id = contrato.Id,
                    NumeroContrato = contrato.NumeroContrato.ToString()
                };
            }
            
            // Telefono Tienda
            long telefonoTienda; // ELIMINAR ESTO. EL NUMERO DEBE ENVIARSE COMO STRING
            Int64.TryParse(tiendaBD.Telefono, out telefonoTienda);

            // Descripción Tienda
            string descripcionTienda = "";
            if (tiendaBD.Descripcion != null) descripcionTienda = tiendaBD.Descripcion;

            // FanPage Tienda
            string fanPageTienda = "";
            if (tiendaBD.Fanpage != null) fanPageTienda = tiendaBD.Fanpage;

            // Crear un objeto de la tienda View Model
            tienda = new TiendaViewModel
            {
                Id = tiendaBD.Id,
                Nombre = tiendaBD.Nombre,
                Imagenes = imagenTienda,
                Categoria = new CategoriaViewModel
                {
                    Id = infoCategoria.Id,
                    Nombre = infoCategoria.Nombre,
                    Imagen = imagenCategoria,
                },
                ImagenesBanner = imagenesBanner,
                Ubicaciones = ubicacionTienda,
                Telefono = telefonoTienda,
                Horario = tiendaBD.Horario,
                FanPage = fanPageTienda,
                Descripcion = descripcionTienda,
                Estado = tiendaBD.Estado,
                Zona = (ubicacionTienda.Count > 0) ? ubicacionTienda[0].NombreZona: "",
                Nivel = (ubicacionTienda.Count > 0) ? ubicacionTienda[0].NombreNivel: "",
                Sector = (ubicacionTienda.Count > 0) ? ubicacionTienda[0].NombreZona: "",
                Contrato = contratoView,
                Etiquetas = etiquetasTienda,
                Local = tiendaBD.Local
            };

            // Retornar objeto tienda
            return tienda;
        }

        public EventoViewModel obtenerEvento(int id)
        {
            // Intancia de un lista de ofertas
            EventoViewModel Evento = new EventoViewModel();

            // Obtener evento de la base de datos
            var EventosBD = ConexionDatabase.Eventos.Where(x => x.Id == id).FirstOrDefault();
           
            // Obtener Ubicacion de la oferta
            var ubicacion = obtenerUbicacion(EventosBD.UbicacionId);

            // Nivel Zona (ELIMINAR)
            NivelZonaViewModel nivelZona = new NivelZonaViewModel();
            nivelZona.Id = ubicacion.Id;
            nivelZona.IdNivel = ubicacion.IdNivel;
            nivelZona.IdZona = ubicacion.IdZona;
            nivelZona.NombreZona = ubicacion.NombreZona;
            nivelZona.NombreNivel = ubicacion.NombreNivel;
            nivelZona.Color = "";

            // Obtener Imagen de la oferta
            var imagen = obtenerImagen(EventosBD.ImagenId);

            // Cambiar fecha de inicio y fin a string
            var espanol = new System.Globalization.CultureInfo("es-MX");
            string inicio = EventosBD.FechaInicio.ToString("yyyy/MM/dd");
            string horaInicio = EventosBD.FechaInicio.ToString("hh:mm:ss");
            string fin = EventosBD.FechaFin.ToString("yyyy/MM/dd");
            string horaFin = EventosBD.FechaFin.ToString("hh:mm:ss");
            string numeroDiaInicio = EventosBD.FechaInicio.ToString("dd");
            string numeroDiaFin = EventosBD.FechaFin.ToString("dd");
            string nombreDiaInicio = (espanol.DateTimeFormat.DayNames[(int)EventosBD.FechaInicio.DayOfWeek]).Substring(0, 3);
            string NombreMesInicio = (espanol.DateTimeFormat.GetAbbreviatedMonthName(EventosBD.FechaFin.Month));
            string nombreDiaFin = (espanol.DateTimeFormat.DayNames[(int)EventosBD.FechaFin.DayOfWeek]).Substring(0, 3);
            string NombreMesFin = (espanol.DateTimeFormat.GetAbbreviatedMonthName(EventosBD.FechaFin.Month));
            string detalleFecha = "Del " + nombreDiaInicio + " " + numeroDiaInicio + " de " + NombreMesInicio + "\n Al " + nombreDiaFin + " " + numeroDiaFin + " de " + NombreMesFin + "\n" + horaInicio + " - " + horaFin;

            // Obtener la tienda asociada
            TiendaViewModel tienda = obtenerTienda(EventosBD.TiendaId);

            // Agregar a la lista el evento
            Evento = new EventoViewModel
            {
                Id = EventosBD.Id,
                Nombre = EventosBD.Nombre,
                FechaInicioPublicacion = EventosBD.FechaInicioPublicacion,
                FechaFinPublicacion = EventosBD.FechaFinPublicacion,
                FechaInicio = inicio,
                FechaFin = fin,
                HoraInicio = horaInicio,
                HoraFin = horaFin,
                Descripcion = EventosBD.Descripcion,
                Estado = EventosBD.Estado,
                Imagen = imagen,
                Ubicacion = ubicacion,
                Tienda = tienda,
                NivelZona = nivelZona,
                DetalleFecha = detalleFecha,
            };

            // Retornar Evento
            return Evento;
        }

        public OfertaViewModel obtenerOferta(int id)
        {
            // Intancia de un lista de ofertas
            OfertaViewModel Oferta = new OfertaViewModel();

            // Obtener las ofertas de la base de datos
            var listaOfertasBD = ConexionDatabase.Ofertas.Where(x => x.Id == id).FirstOrDefault();

            // Obtener Ubicacion de la oferta
            var ubicacion = obtenerUbicacion(listaOfertasBD.UbicacionId);

            // Nivel Zona (ELIMINAR)
            NivelZonaViewModel nivelZona = new NivelZonaViewModel();
            nivelZona.Id = ubicacion.Id;
            nivelZona.IdNivel = ubicacion.IdNivel;
            nivelZona.IdZona = ubicacion.IdZona;
            nivelZona.NombreZona = ubicacion.NombreZona;
            nivelZona.NombreNivel = ubicacion.NombreNivel;
            nivelZona.Color = "";

            // Obtener Imagen de la oferta
            var imagen = obtenerImagen(listaOfertasBD.ImagenId);

            // Cambiar fecha de inicio y fin a string
            var espanol = new System.Globalization.CultureInfo("es-MX");
            string inicio = listaOfertasBD.FechaInicio.ToString("yyyy/MM/dd");
            string horaInicio = listaOfertasBD.FechaInicio.ToString("hh:mm:ss");
            string fin = listaOfertasBD.FechaFin.ToString("yyyy/MM/dd");
            string horaFin = listaOfertasBD.FechaFin.ToString("hh:mm:ss");
            string numeroDiaInicio = listaOfertasBD.FechaInicio.ToString("dd");
            string numeroDiaFin = listaOfertasBD.FechaFin.ToString("dd");
            string nombreDiaInicio = (espanol.DateTimeFormat.DayNames[(int)listaOfertasBD.FechaInicio.DayOfWeek]).Substring(0, 3);
            string NombreMesInicio = (espanol.DateTimeFormat.GetAbbreviatedMonthName(listaOfertasBD.FechaFin.Month));
            string nombreDiaFin = (espanol.DateTimeFormat.DayNames[(int)listaOfertasBD.FechaFin.DayOfWeek]).Substring(0, 3);
            string NombreMesFin = (espanol.DateTimeFormat.GetAbbreviatedMonthName(listaOfertasBD.FechaFin.Month));
            string detalleFecha = "Del " + nombreDiaInicio + " " + numeroDiaInicio + " de " + NombreMesInicio + "\n Al " + nombreDiaFin + " " + numeroDiaFin + " de " + NombreMesFin + "\n" + horaInicio + " - " + horaFin;


            // Validar si el oferta es de jockey
            TiendaViewModel tienda = obtenerTienda(listaOfertasBD.TiendaId);

            //obtener el telefono de la oferta
            var telefonoTienda = ConexionDatabase.Tiendas.Where(x => x.Id == listaOfertasBD.TiendaId).Select(t => t.Telefono).FirstOrDefault();  
            
            // Telefono oferta
            long telefonoOferta; // ELIMINAR ESTO. EL NUMERO DEBE ENVIARSE COMO STRING
            Int64.TryParse(telefonoTienda, out telefonoOferta);

            // Agregar a la lista el oferta
            Oferta = new OfertaViewModel
            {
                Id = listaOfertasBD.Id,
                Nombre = listaOfertasBD.Nombre,
                FechaInicioPublicacion = listaOfertasBD.FechaInicioPublicacion,
                FechaFinPublicacion = listaOfertasBD.FechaFinPublicacion,
                FechaInicio = inicio,
                FechaFin = fin,
                TerminoCondiciones = listaOfertasBD.TerminoCondiciones,
                Telefono = telefonoOferta,
                HoraInicio = horaInicio,
                HoraFin = horaFin,                
                Descripcion = listaOfertasBD.Descripcion,
                Estado = listaOfertasBD.Estado,
                Imagen = imagen,
                Ubicacion = ubicacion,
                Tienda = tienda,
                NivelZona = nivelZona,
                DetalleFecha = detalleFecha,
            };

            // Retornar oferta
            return Oferta;
        }

        public ServicioViewModel obtenerServicio(int id)
        {
            // Obtener el servicio
            Servicio servicioBD = ConexionDatabase.Servicios.Where(x => x.Id == id).FirstOrDefault();
            
            // Categoría
            CategoriaServicio categoriaBD = ConexionDatabase.CategoriasServicios.Where(x => x.Id == servicioBD.CategoriaServicioId).FirstOrDefault();
            CategoriaServicioViewModel categoria = new CategoriaServicioViewModel
            {
                Id = categoriaBD.Id,
                Nombre = categoriaBD.Nombre
            };

            // Obtener las imagenes
            ImagenViewModel imagen = obtenerImagen(servicioBD.ImagenId);
            ImagenViewModel imagen2 = obtenerImagen(servicioBD.Imagen2Id);

            // Obtener el banner
            List<ImagenBannerViewModel> listaImagenesBanner = obtenerImagenesBanner((int)IModulo.Servicios, servicioBD.Id);

            // Ubicaciones
            List<UbicacionViewModel> ubicaciones = obtenerUbicacionesElemento((int)IModulo.Servicios, servicioBD.Id);

            // Crear el servicio
            ServicioViewModel servicio = new ServicioViewModel
            {
                Id = servicioBD.Id,
                Nombre = servicioBD.Nombre,
                Descripcion = servicioBD.Descripcion,
                Horario = servicioBD.Horario,
                WhatsApp = Convert.ToInt64(servicioBD.WhatsApp),
                Categoria = categoria,
                Imagenes = imagen,
                Imagen2 = imagen2,
                ImagenesBanner = listaImagenesBanner,
                ServiciosUbicacion = ubicaciones
            };

            // Retorno
            return servicio;
        }

        public CarteleraViewModel obtenerCatelera(int id)
        {
            //Instanciar funciones de la cartelera
            List<FuncionViewModel> listaFunciones = new List<FuncionViewModel>();

            // Obtener el cartelera
            Cartelera carteleraBD = ConexionDatabase.Cartelera.Where(x => x.Id == id).FirstOrDefault();

            // Obtener las imagenes
            ImagenViewModel imagen = obtenerImagen(carteleraBD.ImagenId);

            // Obtener las funciones de la cartelera
            var FuncionesBD = ConexionDatabase.Funciones.Where(x => x.CarteleraId == id).ToList();

            FuncionesBD.ForEach(y =>
            {
                listaFunciones.Add(new FuncionViewModel
                {
                    Id = y.Id,
                    Sala = y.Sala,
                    Fecha = y.Fecha,
                    Dia = y.Dia,
                    HoraFin = y.HoraFin,
                    HoraInicio = y.HoraInicio,
                });
            });

            // Crear el cartelera
            CarteleraViewModel cartelera = new CarteleraViewModel
            {
                Id = carteleraBD.Id,
                Nombre = carteleraBD.Nombre,
                Descripcion = carteleraBD.Descripcion,
                Imagen = imagen,
                Funcion = listaFunciones
            };

            // Retorno
            return cartelera;
        }

        // Función para obtener las etiquetas de un elemento
        public List<EtiquetaViewModel> obtenerEtiquetasElemento(int idmodulo, int id)
        {
            // Listado de etiquetas a retornar
            List<EtiquetaViewModel> etiquetas = new List<EtiquetaViewModel>();

            // Obtener las etiquetas asociadas al elemento
            var etiquetaElementoBD = (from t in ConexionDatabase.Etiquetas
                                      join ee in ConexionDatabase.EtiquetasElemento on t.Id equals ee.EtiquetaId
                                      join e in ConexionDatabase.Elementos on ee.ElementoId equals e.Id
                                      where e.ModuloId == idmodulo && e.NumeroElemento == id
                                      select new { etiqueta = t }).ToList();
            // Validar Consulta.
            if (etiquetaElementoBD == null) return etiquetas;
            etiquetaElementoBD.ForEach(x =>
            {
                // Armar el objeto
                EtiquetaViewModel etiqueta = new EtiquetaViewModel
                {
                    Id = x.etiqueta.Id,
                    Etiqueta = x.etiqueta.Nombre
                };
                etiquetas.Add(etiqueta);
            });

            // Retorno
            return etiquetas;
        }
    }
}
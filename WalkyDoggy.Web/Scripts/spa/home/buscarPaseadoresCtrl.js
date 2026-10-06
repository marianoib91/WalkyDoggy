(function (app) {
    'use strict';

    app.controller('buscarPaseadoresCtrl', buscarPaseadoresCtrl);

    buscarPaseadoresCtrl.$inject = ['$scope', '$rootScope', '$location', '$window', 'servicioApi', 'servicioNotificaciones', 'servicioFavoritos', 'servicioCaracteristicas'];

    //Portada del cliente, en dos pasos (como el registro del paseador):
    //1. Que busca: desde donde se retira a las mascotas, cuales pasean y, si lo tiene, el dia y/o el horario que prefiere (todo opcional).
    //2. Los paseadores de la zona que cumplen con eso (llevan tantos perros como mascotas se eligieron y tienen lugar), con sus ordenes
    //   (por defecto, por valoracion) y filtros (incluido el matching entre mascotas).
    function buscarPaseadoresCtrl($scope, $rootScope, $location, $window, servicioApi, servicioNotificaciones, servicioFavoritos, servicioCaracteristicas) {
        var idCliente = $rootScope.repository.loggedUser.customerId;
        var idUsuario = $rootScope.repository.loggedUser.id;
        var numeroBusqueda = 0;

        //Lo que el cliente habia elegido la ultima vez (por ejemplo, si vuelve desde la reserva): se recupera al volver a la portada
        var anterior = $rootScope.busquedaPaseo && $rootScope.busquedaPaseo.idCliente === idCliente ? $rootScope.busquedaPaseo : null;

        $scope.paso = 1;
        $scope.nombresPasos = ['Qué buscás', 'Elegí al paseador'];

        $scope.cliente = null;
        $scope.domicilio = '';
        $scope.domicilioUbicado = false;

        //Donde se retira a las mascotas: en el domicilio del cliente ('home') o en otra direccion ('other')
        $scope.retiro = anterior ? anterior.retiro : { modo: 'home', otro: {} };

        //Dia y horario preferidos (los dos son opcionales): solo el dia, dia y hora, solo la hora (cualquier dia) o ninguno (todos los paseadores de la zona)
        $scope.busqueda = anterior ? anterior.busqueda : { fecha: null, hora: '' };
        $scope.mascotas = [];
        $scope.hoy = moment().format('YYYY-MM-DD');
        $scope.horasDelDia = [];
        for (var hora = 0; hora < 24; hora++) {
            $scope.horasDelDia.push((hora < 10 ? '0' : '') + hora + ':00');
        }

        $scope.paseadores = [];
        $scope.paseadoresOrdenados = [];
        $scope.ordenarPor = 'rating';

        //Filtros: cuantos perros a la vez lleva como maximo el paseador (1 = paseo individual), solo favoritos y solo con perros parecidos
        $scope.filtros = { perros: '', soloFavoritos: false, soloParecidos: false };
        $scope.opcionesPerros = [
            { valor: '', texto: 'Cualquiera' },
            { valor: 1, texto: 'Solo 1 perro (paseo individual)' },
            { valor: 2, texto: 'Hasta 2 perros' },
            { valor: 3, texto: 'Hasta 3 perros' },
            { valor: 4, texto: 'Hasta 4 perros' }
        ];
        $scope.buscando = false;
        $scope.buscado = false;
        $scope.pagosPendientes = 0;

        iniciar();

        function iniciar() {
            //Los favoritos del cliente, para marcarlos en la lista y poder filtrar por ellos
            servicioFavoritos.cargar(idCliente, function () {
                //Si ya estaban cargados la respuesta es inmediata y el orden todavia no esta definido: se aplica al buscar
                if ($scope.establecerOrden && $scope.buscado) {
                    $scope.establecerOrden($scope.ordenarPor);
                }
            });

            servicioApi.get('/api/customers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
                $scope.cliente = resultado.data;
                $scope.domicilio = textoDireccion(resultado.data);
                $scope.domicilioUbicado = leerCoordenadas(resultado.data) !== null;

                //Si el domicilio no esta ubicado en el mapa, se arranca pidiendo otra direccion de retiro
                if (!$scope.domicilioUbicado && !anterior) {
                    $scope.retiro.modo = 'other';
                }
            });

            //Las mascotas del cliente: se eligen cuales pasean (y con ellas se busca el matching entre mascotas)
            servicioApi.get('/api/pets/getAllByCustomerId/', { params: { customerId: idCliente } }, function (resultado) {
                $scope.mascotas = resultado.data;
                $scope.mascotasCargadas = true;

                //Se recuperan las de la ultima vez; si tiene una sola mascota, ya queda elegida
                angular.forEach($scope.mascotas, function (mascota) {
                    mascota.seleccionada = anterior ? anterior.idsMascotas.indexOf(mascota.id) !== -1 : $scope.mascotas.length === 1;
                });
            });

            //Se avisa si hay paseos terminados que todavia no pago
            servicioApi.get('/api/walks/getBookingsForCustomer', { params: { customerId: idCliente } }, function (resultado) {
                $scope.pagosPendientes = resultado.data.filter(function (reserva) {
                    return reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending';
                }).length;
            });
        }

        /* ---------- Direccion de retiro ---------- */

        function leerCoordenadas(direccion) {
            var latitud = parseFloat(direccion && direccion.latitude);
            var longitud = parseFloat(direccion && direccion.longitude);
            return (isNaN(latitud) || isNaN(longitud)) ? null : { latitud: latitud, longitud: longitud };
        }

        function textoDireccion(direccion) {
            var linea = [direccion.streetName, direccion.streetNumber].filter(Boolean).join(' ');
            return [linea, direccion.cityName, direccion.provinceName].filter(Boolean).join(', ');
        }

        //La direccion elegida para el retiro (null si eligio "otra direccion" y todavia no la ubico)
        function direccionElegida() {
            var esDomicilio = $scope.retiro.modo !== 'other';
            var origen = esDomicilio ? $scope.cliente : $scope.retiro.otro;

            if (!origen || leerCoordenadas(origen) === null) {
                return null;
            }

            return {
                isHome: esDomicilio,
                streetName: origen.streetName,
                streetNumber: origen.streetNumber,
                cityId: origen.cityId,
                cityName: origen.cityName,
                provinceName: origen.provinceName,
                latitude: origen.latitude,
                longitude: origen.longitude
            };
        }

        //Que le falta a la direccion de "otra direccion" para poder reservar (null si esta completa o si se retira en el domicilio).
        //La busqueda alcanza con la ubicacion del mapa, pero la reserva necesita calle, numero y ciudad reconocida.
        $scope.faltaEnRetiro = function () {
            var otro = $scope.retiro.otro;
            if ($scope.retiro.modo !== 'other' || leerCoordenadas(otro) === null) {
                return null;
            }

            var faltantes = [];
            if (!otro.streetName) {
                faltantes.push('la calle');
            }
            if (!(Number(otro.streetNumber) > 0)) {
                faltantes.push('el número de la calle');
            }
            if (!otro.cityId) {
                faltantes.push('la ciudad (elegí la dirección de la lista de sugerencias)');
            }

            return faltantes.length ? faltantes.join(' y ') : null;
        };

        $scope.textoRetiro = function () {
            var direccion = direccionElegida();
            return direccion ? textoDireccion(direccion) : '';
        };

        /* ---------- Mascotas ---------- */

        //Las mascotas que van al paseo
        function mascotasElegidas() {
            return $scope.mascotas.filter(function (mascota) { return mascota.seleccionada; });
        }

        $scope.idsMascotasElegidas = function () {
            return mascotasElegidas().map(function (mascota) { return mascota.id; }).join(',');
        };

        //Las caracteristicas de la mascota en castellano (["Juguetón", "Corredor"])
        $scope.rasgosDe = function (mascota) {
            return servicioCaracteristicas.leer(mascota.traits).map(servicioCaracteristicas.textoDe);
        };

        //"Rex" o "Rex y Luna"
        function unirNombres(mascotas) {
            var nombres = mascotas.map(function (mascota) { return mascota.name; });
            return nombres.length > 1 ? nombres.slice(0, -1).join(', ') + ' y ' + nombres[nombres.length - 1] : nombres.join('');
        }

        $scope.nombresMascotas = function () {
            return unirNombres(mascotasElegidas());
        };

        /* ---------- Paso 1: que busca ---------- */

        //Se puede pasar al paso 2 con una direccion ubicada, al menos una mascota y, si busca por dia, la fecha
        $scope.continuar = function () {
            if (!direccionElegida()) {
                servicioNotificaciones.mostrarError('Elegí desde dónde retiramos a tus mascotas.');
                return;
            }
            if (mascotasElegidas().length === 0) {
                servicioNotificaciones.mostrarError('Elegí al menos una mascota para el paseo.');
                return;
            }

            guardarBusqueda();

            //Siempre se empieza ordenando por valoracion; despues se puede cambiar el orden
            $scope.ordenarPor = 'rating';
            $scope.filtros = { perros: '', soloFavoritos: false, soloParecidos: false };

            $scope.paso = 2;
            $scope.buscado = false;
            buscar();
            $window.scrollTo(0, 0);
        };

        //El paso 2 vuelve al 1 con todo lo que habia elegido
        $scope.volver = function () {
            $scope.paso = 1;
            $window.scrollTo(0, 0);
        };

        function guardarBusqueda() {
            $rootScope.busquedaPaseo = {
                idCliente: idCliente,
                retiro: $scope.retiro,
                busqueda: $scope.busqueda,
                idsMascotas: mascotasElegidas().map(function (mascota) { return mascota.id; })
            };
        }

        /* ---------- Paso 2: busqueda ---------- */

        function buscar() {
            var direccion = direccionElegida();
            var numero = ++numeroBusqueda;

            if (!direccion) {
                $scope.paseadores = [];
                $scope.paseadoresOrdenados = [];
                $scope.buscando = false;
                $scope.buscado = false;
                return;
            }

            var parametros = { latitude: direccion.latitude, longitude: direccion.longitude };
            if ($scope.busqueda.fecha) {
                parametros.date = moment($scope.busqueda.fecha).format('YYYY-MM-DD');
            }
            if ($scope.busqueda.hora) {
                parametros.timeFrom = $scope.busqueda.hora;
            }

            //El servidor deja solo a los paseadores que llevan tantos perros como mascotas se eligieron (y que tienen lugar para todas)
            parametros.petIds = $scope.idsMascotasElegidas();

            $scope.buscando = true;
            servicioApi.get('/api/walkers/getForPickup', { params: parametros }, function (resultado) {
                if (numero !== numeroBusqueda) {
                    return;
                }
                $scope.paseadores = resultado.data;
                $scope.buscando = false;
                $scope.buscado = true;
                $scope.establecerOrden($scope.ordenarPor);
            }, function (error) {
                if (numero !== numeroBusqueda) {
                    return;
                }
                $scope.buscando = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo buscar paseadores. Intentá nuevamente.');
            });
        }

        //Se aplican los filtros (perros a la vez, favoritos, perros parecidos) y despues el orden:
        //distance: de menor a mayor distancia a la direccion de retiro.
        //amount: de menor a mayor tarifa por hora; a igual tarifa, el mas cercano.
        //rating: de mayor a menor promedio; a igual promedio, el que tiene mas valoraciones; los que no tienen, al final.
        //matching (solo por dia y horario, cuando todos tienen lugar): de mayor a menor cantidad de caracteristicas en comun con el perro mas parecido
        //que lleva el paseador; a igual cantidad, el que lleva mas perros parecidos, y despues el mas cercano.
        $scope.establecerOrden = function (ordenarPor) {
            $scope.ordenarPor = ordenarPor;

            var maximoPerros = Number($scope.filtros.perros);
            var lista = $scope.paseadores.filter(function (paseador) {
                return (!maximoPerros || paseador.maxPetsAtOnce <= maximoPerros) &&
                       (!$scope.filtros.soloFavoritos || servicioFavoritos.esFavorito(paseador.id)) &&
                       (!$scope.filtros.soloParecidos || paseador.matchingPets > 0);
            });
            lista.sort(function (a, b) {
                if (ordenarPor === 'distance') {
                    return (a.distanceKm || 0) - (b.distanceKm || 0);
                }
                if (ordenarPor === 'amount') {
                    return (a.amount - b.amount) || ((a.distanceKm || 0) - (b.distanceKm || 0));
                }
                if (ordenarPor === 'matching') {
                    return ((b.matchScore || 0) - (a.matchScore || 0)) || ((b.matchingPets || 0) - (a.matchingPets || 0)) || ((a.distanceKm || 0) - (b.distanceKm || 0));
                }

                var valoradoA = a.averageRating !== null && a.averageRating !== undefined;
                var valoradoB = b.averageRating !== null && b.averageRating !== undefined;
                if (valoradoA !== valoradoB) { return valoradoA ? -1 : 1; }
                if (valoradoA && a.averageRating !== b.averageRating) { return b.averageRating - a.averageRating; }
                if (a.ratingCount !== b.ratingCount) { return b.ratingCount - a.ratingCount; }
                return (a.distanceKm || 0) - (b.distanceKm || 0);
            });

            $scope.paseadoresOrdenados = lista;
        };

        $scope.textoDistancia = function (paseador) {
            if (paseador.distanceKm === null || paseador.distanceKm === undefined) {
                return '';
            }
            return paseador.distanceKm < 0.1 ? 'a menos de 100 m' : 'a ' + String(paseador.distanceKm).replace('.', ',') + ' km';
        };

        /* ---------- Matching entre mascotas ---------- */

        //De las mascotas elegidas, las que tienen las caracteristicas necesarias para comparar
        function mascotasConRasgos() {
            return mascotasElegidas().filter(function (mascota) {
                return servicioCaracteristicas.leer(mascota.traits).length >= servicioCaracteristicas.minimoEnComun;
            });
        }

        //Si a las mascotas elegidas les faltan caracteristicas, no se puede buscar compañeros parecidos
        $scope.faltanRasgos = function () {
            return mascotasElegidas().length > 0 && mascotasConRasgos().length === 0;
        };

        $scope.minimoRasgos = servicioCaracteristicas.minimoEnComun;

        //Se eligio un dia y/o un horario: los paseadores traen sus horarios libres
        $scope.buscaPorHorario = function () {
            return !!($scope.busqueda.fecha || $scope.busqueda.hora);
        };

        //Hay matching para mostrar: alguna mascota elegida tiene las caracteristicas necesarias para compararla
        //(con dia y/o horario se miran esos horarios; sin ninguno, los proximos dias de cada paseador)
        $scope.hayMatching = function () {
            return mascotasConRasgos().length > 0;
        };

        //"Rex" o "Rex y Luna": las mascotas para las que se compara
        $scope.nombresElegidas = function () {
            return unirNombres(mascotasConRasgos());
        };

        //Con mas de una mascota se aclara con cual coincide cada perro
        $scope.variasMascotas = function () {
            return mascotasConRasgos().length > 1;
        };

        //Los horarios del paseador en los que lleva perros parecidos, del que mas se parece al que menos (a igual parecido, el mas temprano)
        $scope.coincidencias = function (paseador) {
            return (paseador.availableTimes || []).filter(function (cupo) { return cupo.matchingPets > 0; }).sort(function (a, b) {
                return (b.matchScore - a.matchScore) || ((a.date + a.time) < (b.date + b.time) ? -1 : 1);
            });
        };

        $scope.coincidenciasVisibles = function (paseador) {
            var todas = $scope.coincidencias(paseador);
            return paseador.verTodasCoincidencias ? todas : todas.slice(0, 3);
        };

        //"Pitbull, grande"
        $scope.textoPerro = function (coincidencia) {
            return [coincidencia.breedName, coincidencia.sizeName ? coincidencia.sizeName.toLowerCase() : null].filter(Boolean).join(', ');
        };

        //Cuantos de los paseadores encontrados llevan perros parecidos
        $scope.cantidadConMatching = function () {
            return $scope.paseadores.filter(function (paseador) { return paseador.matchingPets > 0; }).length;
        };

        //Los horarios que se muestran de un paseador: los primeros 8 (o todos), y los sugeridos aunque queden mas alla de los 8
        $scope.horariosVisibles = function (paseador) {
            var horarios = paseador.availableTimes;
            if (paseador.verTodos) {
                return horarios;
            }
            return horarios.filter(function (cupo, indice) {
                return indice < 8 || ($scope.hayMatching() && cupo.matchingPets > 0);
            });
        };

        $scope.textoRasgos = function (codigos) {
            return servicioCaracteristicas.textos(codigos);
        };

        /* ---------- Favoritos y datos de cada paseador ---------- */

        $scope.esFavorito = function (paseador) {
            return servicioFavoritos.esFavorito(paseador.id);
        };

        //Cuantos de los paseadores de esta busqueda son favoritos del cliente
        $scope.cantidadFavoritos = function () {
            return $scope.paseadores.filter(function (paseador) { return servicioFavoritos.esFavorito(paseador.id); }).length;
        };

        $scope.alternarFavorito = function (paseador) {
            servicioFavoritos.alternar(idCliente, paseador.id, function () {
                //Si se esta filtrando por favoritos, el que se desmarca sale de la lista
                $scope.establecerOrden($scope.ordenarPor);
            });
        };

        $scope.textoPerros = function (paseador) {
            return paseador.maxPetsAtOnce === 1 ? 'pasea de a un perro (paseo individual)' : 'pasea hasta ' + paseador.maxPetsAtOnce + ' perros a la vez';
        };

        $scope.textoRadio = function (paseador) {
            return String(paseador.serviceRadiusKm).replace('.', ',');
        };

        //"quedan 2 lugares", "último lugar"
        $scope.textoLugares = function (cupo) {
            return cupo.freeSpots === 1 ? 'último lugar' : 'quedan ' + cupo.freeSpots + ' lugares';
        };

        var nombresDiasCortos = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];

        //"lun 12/10"
        $scope.textoDia = function (fecha) {
            var dia = moment(fecha, 'YYYY-MM-DD');
            return nombresDiasCortos[dia.day()] + ' ' + dia.format('DD/MM');
        };

        //El horario de un cupo; si no se eligio un dia, se aclara el dia ("lun 12/10 · 13:00")
        $scope.textoCuando = function (cupo) {
            return $scope.busqueda.fecha ? cupo.time : $scope.textoDia(cupo.date) + ' · ' + cupo.time;
        };

        $scope.cantidadMascotasElegidas = function () {
            return mascotasElegidas().length;
        };

        //Titulo de los horarios de cada paseador: "Horarios con lugar el 05/10/2026" o, sin dia, "Días con lugar a las 13:00"
        $scope.tituloHorarios = function () {
            return $scope.busqueda.fecha
                ? 'Horarios con lugar el ' + moment($scope.busqueda.fecha).format('DD/MM/YYYY')
                : 'Días con lugar a las ' + $scope.busqueda.hora;
        };

        //Lo que se busco, para el resumen: "Cualquier día y horario", "El 05/10/2026", "El 05/10/2026 a las 13:00", "A las 13:00, cualquier día"
        $scope.textoHorarioBuscado = function () {
            var fecha = $scope.busqueda.fecha ? moment($scope.busqueda.fecha).format('DD/MM/YYYY') : null;
            var hora = $scope.busqueda.hora || null;

            if (fecha && hora) { return 'El ' + fecha + ' a las ' + hora; }
            if (fecha) { return 'El ' + fecha; }
            if (hora) { return 'A las ' + hora + ', cualquier día de los próximos 14'; }
            return 'Cualquier día y horario';
        };

        /* ---------- Pedir el paseo ---------- */

        //Los datos elegidos (direccion de retiro, mascotas y, si los puso, dia y horario) viajan al paso 1 de la reserva.
        //Si se toca un horario de la lista de un paseador (cupo), se reserva ese dia y horario.
        $scope.solicitar = function (paseador, cupo) {
            var direccion = direccionElegida();
            if (!direccion) {
                return;
            }

            var falta = $scope.faltaEnRetiro();
            if (falta) {
                servicioNotificaciones.mostrarError('A la dirección de retiro le falta ' + falta + '. Volvé al paso anterior y completala para poder reservar.');
                return;
            }

            //Una busqueda nueva descarta cualquier reserva a medio hacer de antes
            $rootScope.borradorPaseo = null;

            //Sin un horario tocado, se propone lo que se busco; si solo se busco la hora, el primer dia en que el paseador tiene lugar a esa hora
            var propuesto = cupo || (!$scope.busqueda.fecha && $scope.busqueda.hora && paseador.availableTimes && paseador.availableTimes[0]) || null;
            $rootScope.retiroElegido = {
                pickup: direccion,
                fecha: propuesto ? propuesto.date : ($scope.busqueda.fecha ? moment($scope.busqueda.fecha).format('YYYY-MM-DD') : null),
                hora: propuesto ? propuesto.time : ($scope.busqueda.hora || null),
                idsMascotas: mascotasElegidas().map(function (mascota) { return mascota.id; })
            };

            $location.path('/walks/step-1').search({ walkerId: paseador.id });
        };
    }

})(angular.module('walkyDoggy'));

(function (app) {
    'use strict';

    app.controller('buscarPaseadoresCtrl', buscarPaseadoresCtrl);

    buscarPaseadoresCtrl.$inject = ['$scope', '$rootScope', '$location', '$timeout', 'servicioApi', 'servicioNotificaciones'];

    //Portada del cliente: primero elige desde que direccion se retira a las mascotas y despues busca a un paseador
    //que trabaje en esa zona, por cercania/valoracion o por el dia y horario en que quiere el paseo.
    function buscarPaseadoresCtrl($scope, $rootScope, $location, $timeout, servicioApi, servicioNotificaciones) {
        var idCliente = $rootScope.repository.loggedUser.customerId;
        var idUsuario = $rootScope.repository.loggedUser.id;
        var temporizador = null;
        var numeroBusqueda = 0;

        $scope.cliente = null;
        $scope.domicilio = '';
        $scope.domicilioUbicado = false;

        //Donde se retira a las mascotas: en el domicilio del cliente ('home') o en otra direccion ('other')
        $scope.retiro = { modo: 'home', otro: {} };

        //Como se elige al paseador: por zona ('zona', ordenado por cercania o valoracion) o por dia y horario ('horario')
        $scope.busqueda = { modo: 'zona', fecha: diaSiguiente(), hora: '' };
        $scope.hoy = moment().format('YYYY-MM-DD');
        $scope.horasDelDia = [];
        for (var hora = 0; hora < 24; hora++) {
            $scope.horasDelDia.push((hora < 10 ? '0' : '') + hora + ':00');
        }

        $scope.paseadores = [];
        $scope.paseadoresOrdenados = [];
        $scope.ordenarPor = 'distance';

        //Filtro: cuantos perros a la vez lleva como maximo el paseador (1 = paseo individual)
        $scope.filtros = { perros: '' };
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
            servicioApi.get('/api/customers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
                $scope.cliente = resultado.data;
                $scope.domicilio = textoDireccion(resultado.data);
                $scope.domicilioUbicado = leerCoordenadas(resultado.data) !== null;

                //Si el domicilio no esta ubicado en el mapa, se arranca pidiendo otra direccion de retiro
                if (!$scope.domicilioUbicado) {
                    $scope.retiro.modo = 'other';
                }
                buscar();
            });

            //Se avisa si hay paseos terminados que todavia no pago
            servicioApi.get('/api/walks/getBookingsForCustomer', { params: { customerId: idCliente } }, function (resultado) {
                $scope.pagosPendientes = resultado.data.filter(function (reserva) {
                    return reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending';
                }).length;
            });
        }

        /* ---------- Direccion de retiro ---------- */

        function diaSiguiente() {
            return moment().add(1, 'days').startOf('day').toDate();
        }

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

        /* ---------- Busqueda ---------- */

        //Cada vez que cambia la direccion, el modo, el dia o el horario se vuelve a buscar (con una pequeña espera por si sigue escribiendo)
        $scope.$watchGroup(['retiro.modo', 'retiro.otro.latitude', 'retiro.otro.longitude', 'busqueda.modo', 'busqueda.fecha', 'busqueda.hora'], function () {
            if (!$scope.cliente) {
                return;
            }
            $timeout.cancel(temporizador);
            temporizador = $timeout(buscar, 350);
        });

        $scope.$on('$destroy', function () {
            $timeout.cancel(temporizador);
        });

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
            if ($scope.busqueda.modo === 'horario' && $scope.busqueda.fecha) {
                parametros.date = moment($scope.busqueda.fecha).format('YYYY-MM-DD');
                if ($scope.busqueda.hora) {
                    parametros.timeFrom = $scope.busqueda.hora;
                }
            }

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

        //Se aplica el filtro de perros a la vez y despues el orden:
        //distance: de menor a mayor distancia a la direccion de retiro.
        //amount: de menor a mayor tarifa por hora; a igual tarifa, el mas cercano.
        //rating: de mayor a menor promedio; a igual promedio, el que tiene mas valoraciones; los que no tienen, al final.
        $scope.establecerOrden = function (ordenarPor) {
            $scope.ordenarPor = ordenarPor;

            var maximoPerros = Number($scope.filtros.perros);
            var lista = $scope.paseadores.filter(function (paseador) {
                return !maximoPerros || paseador.maxPetsAtOnce <= maximoPerros;
            });
            lista.sort(function (a, b) {
                if (ordenarPor === 'distance') {
                    return (a.distanceKm || 0) - (b.distanceKm || 0);
                }
                if (ordenarPor === 'amount') {
                    return (a.amount - b.amount) || ((a.distanceKm || 0) - (b.distanceKm || 0));
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

        $scope.textoHorarioBuscado = function () {
            if ($scope.busqueda.modo !== 'horario' || !$scope.busqueda.fecha) {
                return '';
            }
            var texto = moment($scope.busqueda.fecha).format('DD/MM/YYYY');
            return $scope.busqueda.hora ? texto + ' a las ' + $scope.busqueda.hora : texto;
        };

        /* ---------- Pedir el paseo ---------- */

        //Los datos elegidos (direccion de retiro y, si los puso, dia y horario) viajan al paso 1 de la reserva.
        //Si se toca un horario de la lista de un paseador, se reserva ese horario.
        $scope.solicitar = function (paseador, horaElegida) {
            var direccion = direccionElegida();
            if (!direccion) {
                return;
            }

            var falta = $scope.faltaEnRetiro();
            if (falta) {
                servicioNotificaciones.mostrarError('A la dirección de retiro le falta ' + falta + '. Completala para poder reservar.');
                return;
            }

            var porHorario = $scope.busqueda.modo === 'horario' && $scope.busqueda.fecha;
            $rootScope.retiroElegido = {
                pickup: direccion,
                fecha: porHorario ? moment($scope.busqueda.fecha).format('YYYY-MM-DD') : null,
                hora: horaElegida || (porHorario && $scope.busqueda.hora ? $scope.busqueda.hora : null)
            };

            $location.path('/walks/step-1').search({ walkerId: paseador.id });
        };
    }

})(angular.module('walkyDoggy'));

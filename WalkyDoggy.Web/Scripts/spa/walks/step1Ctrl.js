(function (app) {
    'use strict';

    app.controller('paso1Ctrl', paso1Ctrl);

    paso1Ctrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', '$rootScope', '$location', '$routeParams'];

    function paso1Ctrl($scope, servicioApi, servicioNotificaciones, $rootScope, $location, $routeParams) {

        //Indice de moment().day(): 0 = domingo
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var idPaseador = $routeParams.walkerId;
        var borrador = $rootScope.borradorPaseo;
        var retiroElegido = $rootScope.retiroElegido;
        var numeroDePedido = 0;
        //Horario con el que se llega (de la busqueda o del paso 2). Se aplica cuando ya llego la lista de horarios del dia:
        //si se carga antes, el desplegable no lo muestra aunque el modelo lo tenga.
        var horaPropuesta = null;

        $scope.mascotas = {};
        $scope.paseo = {};
        $scope.paseador = null;
        $scope.horarios = [];
        $scope.nombresJornadas = [];
        $scope.textoJornadas = '';
        $scope.tieneHorario = true;
        $scope.cargandoHorarios = false;

        //Direccion de retiro: la elegida en la portada (o la del paso 2 si se vuelve de ahi). Si no hay ninguna, el domicilio del cliente.
        $scope.retiroActual = null;
        $scope.textoRetiro = '';
        $scope.fueraDeZona = false;
        $scope.distanciaRetiro = null;

        //El calendario solo habilita los dias en los que el paseador tiene una jornada laboral
        $scope.opcionesCalendario = {
            singleDatePicker: true,
            showDropdowns: true,
            minDate: new Date(),
            isInvalidDate: function (fecha) {
                return $scope.nombresJornadas.indexOf(nombresDias[fecha.day()]) === -1;
            }
        };

        if (!idPaseador) {
            servicioNotificaciones.mostrarError('Elegí un paseador para reservar un paseo.');
            $location.search({}).path('/');
            return;
        }

        //Si se vuelve del paso 2 se conserva lo que ya se habia elegido
        if (borrador && borrador.walkerId == idPaseador) {
            $scope.paseo.date = moment(borrador.date, 'YYYY-MM-DD');
            horaPropuesta = borrador.timeFrom;

            if (borrador.pickup) {
                $scope.retiroActual = angular.copy(borrador.pickup);
            }
        } else if (retiroElegido) {
            if (retiroElegido.pickup) {
                $scope.retiroActual = angular.copy(retiroElegido.pickup);
            }

            //Si en la portada se buscó por día y horario, se propone ese día y horario
            if (retiroElegido.fecha) {
                $scope.paseo.date = moment(retiroElegido.fecha, 'YYYY-MM-DD');
                horaPropuesta = retiroElegido.hora;
            }
        }

        iniciar();

        function iniciar() {
            servicioApi.get('/api/walkers/getDetail', { params: { id: idPaseador } }, alCargarPaseador, alFallarCargaPaseador);
            servicioApi.get('/api/workDays/getAllByWalkerId', { params: { walkerId: idPaseador } }, alCargarJornadas);
            servicioApi.get('/api/customers/getByUserId/', { params: { userId: $rootScope.repository.loggedUser.id } }, alCargarCliente);
        }

        function alCargarPaseador(resultado) {
            $scope.paseador = resultado.data;
            calcularZona();
        }

        function alFallarCargaPaseador() {
            servicioNotificaciones.mostrarError('No se encontró al paseador seleccionado.');
            $location.search({}).path('/');
        }

        function alCargarJornadas(resultado) {
            var nombres = [];
            angular.forEach(resultado.data, function (jornada) {
                if (nombres.indexOf(jornada.dayOfWeek) === -1) {
                    nombres.push(jornada.dayOfWeek);
                }
            });

            $scope.nombresJornadas = nombres;
            $scope.tieneHorario = nombres.length > 0;
            $scope.textoJornadas = nombresDias.filter(function (name) {
                return nombres.indexOf(name) !== -1;
            }).join(', ');

            //Se propone la primera fecha en la que trabaja, a menos que ya haya una elegida
            if (!$scope.paseo.date && $scope.tieneHorario) {
                for (var i = 0; i < 60; i++) {
                    var candidata = moment().add(i, 'days');
                    if (nombres.indexOf(nombresDias[candidata.day()]) !== -1) {
                        $scope.paseo.date = candidata;
                        break;
                    }
                }
            }
        }

        function alCargarCliente(resultado) {
            $scope.cliente = resultado.data;

            //Sin dirección elegida en la portada (por ejemplo, si se llegó desde el perfil del paseador) se retira en el domicilio del cliente
            if (!$scope.retiroActual) {
                $scope.retiroActual = {
                    isHome: true,
                    streetName: $scope.cliente.streetName,
                    streetNumber: $scope.cliente.streetNumber,
                    cityId: $scope.cliente.cityId,
                    cityName: $scope.cliente.cityName,
                    provinceName: $scope.cliente.provinceName,
                    latitude: $scope.cliente.latitude,
                    longitude: $scope.cliente.longitude
                };
            }
            calcularZona();

            servicioApi.get('/api/pets/getAllByCustomerId/', { params: { customerId: $scope.cliente.id } }, alCargarMascotas);
        }

        function alCargarMascotas(resultado) {
            $scope.mascotas = resultado.data;

            if (borrador && borrador.walkerId == idPaseador) {
                var idsSeleccionados = borrador.pets.map(function (mascota) { return mascota.id; });
                angular.forEach($scope.mascotas, function (mascota) {
                    mascota.selectedPet = idsSeleccionados.indexOf(mascota.id) !== -1;
                });
            }
        }

        //Cada vez que cambia la fecha se traen los horarios libres de ese paseador para ese dia
        $scope.$watch('paseo.date', function (fecha) {
            if (!fecha) {
                $scope.horarios = [];
                return;
            }

            var pedido = ++numeroDePedido;
            $scope.cargandoHorarios = true;
            var config = {
                params: {
                    walkerId: idPaseador,
                    date: moment(fecha).format('YYYY-MM-DD')
                }
            };

            servicioApi.get('/api/walkers/getAvailableTimes', config, function (resultado) {
                if (pedido !== numeroDePedido) {
                    return;
                }
                $scope.horarios = resultado.data;
                $scope.cargandoHorarios = false;
                if (horaPropuesta && $scope.horarios.indexOf(horaPropuesta) !== -1) {
                    $scope.paseo.timeFrom = horaPropuesta;
                }
                horaPropuesta = null;

                if ($scope.horarios.indexOf($scope.paseo.timeFrom) === -1) {
                    $scope.paseo.timeFrom = null;
                }
            }, function () {
                if (pedido === numeroDePedido) {
                    $scope.cargandoHorarios = false;
                }
            });
        });

        function formatearDireccion(direccion) {
            var linea = [direccion.streetName, direccion.streetNumber].filter(Boolean).join(' ');
            return [linea, direccion.cityName, direccion.provinceName].filter(Boolean).join(', ');
        }

        //Controla (en el navegador, para avisar antes de reservar) que el retiro quede dentro de la zona de trabajo del paseador;
        //el servidor lo vuelve a controlar al reservar.
        function calcularZona() {
            var retiro = $scope.retiroActual;
            var paseador = $scope.paseador;

            $scope.fueraDeZona = false;
            $scope.distanciaRetiro = null;
            if (!retiro || !paseador) {
                return;
            }

            $scope.textoRetiro = formatearDireccion(retiro);

            var latitudRetiro = parseFloat(retiro.latitude);
            var longitudRetiro = parseFloat(retiro.longitude);
            var latitudPaseador = parseFloat(paseador.latitude);
            var longitudPaseador = parseFloat(paseador.longitude);
            if ([latitudRetiro, longitudRetiro, latitudPaseador, longitudPaseador].some(isNaN) || !(paseador.serviceRadiusKm > 0)) {
                return;
            }

            $scope.distanciaRetiro = Math.round(distanciaEnKilometros(latitudPaseador, longitudPaseador, latitudRetiro, longitudRetiro) * 10) / 10;
            $scope.fueraDeZona = $scope.distanciaRetiro > paseador.serviceRadiusKm;
        }

        //Distancia en linea recta entre dos puntos (formula de Haversine)
        function distanciaEnKilometros(latitud1, longitud1, latitud2, longitud2) {
            var aRadianes = function (grados) { return grados * Math.PI / 180; };
            var deltaLatitud = aRadianes(latitud2 - latitud1);
            var deltaLongitud = aRadianes(longitud2 - longitud1);
            var a = Math.sin(deltaLatitud / 2) * Math.sin(deltaLatitud / 2) +
                    Math.cos(aRadianes(latitud1)) * Math.cos(aRadianes(latitud2)) * Math.sin(deltaLongitud / 2) * Math.sin(deltaLongitud / 2);
            return 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        }

        $scope.enviar = function () {
            var mascotasSeleccionadas = [];
            for (var i = 0; i < $scope.mascotas.length; i++) {
                if ($scope.mascotas[i].selectedPet == true) {
                    mascotasSeleccionadas.push($scope.mascotas[i]);
                }
            }

            if (mascotasSeleccionadas.length === 0) {
                servicioNotificaciones.mostrarError('Debe seleccionar una mascota para su paseo.');
                return;
            }

            var retiro = $scope.retiroActual;
            if (!retiro) {
                servicioNotificaciones.mostrarError('Elegí la dirección de retiro desde la portada.');
                return;
            }
            if ($scope.fueraDeZona) {
                servicioNotificaciones.mostrarError('La dirección de retiro queda fuera de la zona de trabajo de ' + $scope.paseador.firstName + '. Elegí otra dirección o a otro paseador.');
                return;
            }

            var fecha = moment($scope.paseo.date).format('YYYY-MM-DD');
            var criterios = {
                date: fecha,
                timeFrom: $scope.paseo.timeFrom,
                selectedPets: mascotasSeleccionadas
            };

            servicioApi.post('/api/walks/validatePetsInWalks', criterios, function (respuesta) {
                if (respuesta.data.id == 0) {
                    //Los datos elegidos viajan al paso 2 para confirmar la reserva
                    $rootScope.borradorPaseo = {
                        walkerId: idPaseador,
                        date: fecha,
                        timeFrom: $scope.paseo.timeFrom,
                        pickup: retiro,
                        pets: mascotasSeleccionadas.map(function (mascota) {
                            return { id: mascota.id, name: mascota.name, profileImage: mascota.profileImage };
                        })
                    };
                    $location.search({}).path('/walks/step-2');
                }
                else {
                    servicioNotificaciones.mostrarError(respuesta.data.petName + ' ya tiene reservado un paseo a la misma hora y día seleccionados.');
                }
            });
        };
    }

})(angular.module('walkyDoggy'));

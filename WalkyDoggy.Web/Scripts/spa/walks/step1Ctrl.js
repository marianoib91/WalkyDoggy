(function (app) {
    'use strict';

    app.controller('paso1Ctrl', paso1Ctrl);

    paso1Ctrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', '$rootScope', '$location', '$routeParams'];

    function paso1Ctrl($scope, servicioApi, servicioNotificaciones, $rootScope, $location, $routeParams) {

        //Indice de moment().day(): 0 = domingo
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var idPaseador = $routeParams.walkerId;
        var borrador = $rootScope.borradorPaseo;
        var numeroDePedido = 0;

        $scope.mascotas = {};
        $scope.paseo = {};
        $scope.paseador = null;
        $scope.horarios = [];
        $scope.nombresJornadas = [];
        $scope.textoJornadas = '';
        $scope.tieneHorario = true;
        $scope.cargandoHorarios = false;

        //Donde se retira a las mascotas: en el domicilio del cliente ('home') o en otra direccion ('other')
        $scope.retiro = { mode: 'home', other: {} };
        $scope.domicilio = '';

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
            $scope.paseo.timeFrom = borrador.timeFrom;

            if (borrador.pickup && !borrador.pickup.isHome) {
                $scope.retiro.mode = 'other';
                $scope.retiro.other = angular.copy(borrador.pickup);
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
            $scope.domicilio = formatearDireccion($scope.cliente);
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

        //Devuelve la direccion de retiro elegida, o null si eligio "otra direccion" y no la completo
        function armarRetiro() {
            var origen = $scope.cliente;
            var esDomicilioPropio = $scope.retiro.mode !== 'other';

            if (!esDomicilioPropio) {
                origen = $scope.retiro.other;
                if (!origen.cityId || !origen.streetName || !origen.streetNumber) {
                    return null;
                }
            }

            return {
                isHome: esDomicilioPropio,
                streetName: origen.streetName,
                streetNumber: origen.streetNumber,
                cityId: origen.cityId,
                cityName: origen.cityName,
                provinceName: origen.provinceName,
                latitude: origen.latitude,
                longitude: origen.longitude
            };
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

            var retiro = armarRetiro();
            if (!retiro) {
                servicioNotificaciones.mostrarError('Elegí la dirección de retiro de la lista de sugerencias.');
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

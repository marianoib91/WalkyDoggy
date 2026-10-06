(function (app) {
    'use strict';

    app.controller('hospedajeCtrl', hospedajeCtrl);

    hospedajeCtrl.$inject = ['$scope', '$rootScope', '$interval', '$location', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion'];

    //Hospedaje (cliente): busca cuidadores con lugar para unas fechas, pide el hospedaje y sigue los suyos (chat, cancelar, pagar, valorar).
    //Los cuidadores gestionan sus pedidos en "Turnos" y su oferta en "Condiciones laborales".
    function hospedajeCtrl($scope, $rootScope, $interval, $location, servicioApi, servicioNotificaciones, servicioConfirmacion) {
        var usuario = $rootScope.repository.loggedUser;
        var segundosEntreActualizaciones = 20;

        //Un cuidador no tiene esta pantalla: sus hospedajes estan en Turnos
        if (usuario.roleId == '3') {
            $location.path('/');
            return;
        }

        $scope.idCliente = usuario.customerId;
        $scope.trabajando = false;
        $scope.hospedajes = [];
        $scope.cargado = false;

        function mensajeDeError(error, porDefecto) {
            return error && error.data && error.data[0] ? error.data[0] : porDefecto;
        }

        $scope.fecha = function (fecha) {
            return moment(fecha).format('DD/MM/YYYY');
        };

        $scope.dinero = function (monto) {
            return '$' + Number(monto).toLocaleString('es-AR', { minimumFractionDigits: monto % 1 ? 2 : 0, maximumFractionDigits: 2 });
        };

        $scope.textoPerros = function (cantidad) {
            return cantidad + (cantidad === 1 ? ' perro' : ' perros');
        };

        /* ---------- Mis hospedajes ---------- */

        //Las tarjetas (tarjetaHospedajeCtrl) la llaman despues de cada accion
        $scope.recargarHospedajes = cargarHospedajes;

        function cargarHospedajes() {
            servicioApi.get('/api/stays/getForCustomer', { params: { customerId: $scope.idCliente } }, function (resultado) {
                $scope.hospedajes = resultado.data;
                $scope.cargado = true;

                var por = function (estados) {
                    return $scope.hospedajes.filter(function (hospedaje) { return estados.indexOf(hospedaje.status) !== -1; });
                };
                $scope.pendientes = por(['Pending']);
                $scope.vigentes = por(['Upcoming', 'InProgress']);
                $scope.porPagar = por(['ToCollect']);
                $scope.cerrados = por(['Collected', 'Cancelled']);
            });
        }

        //Mercado Pago devuelve al cliente a "Hospedaje?payment=..." cuando termina de pagar
        function mostrarResultadoPago() {
            var resultado = $location.search().payment;
            if (!resultado) {
                return;
            }

            if (resultado === 'approved') {
                servicioNotificaciones.mostrarExito('¡Pago recibido! El dinero ya está en la cuenta de tu cuidador.');
            } else if (resultado === 'pending') {
                servicioNotificaciones.mostrarError('Tu pago todavía no se acreditó. Cuando se confirme lo vas a ver acá. Si Mercado Pago no te funciona, pagale a tu paseador de otra forma y él lo registra.');
            } else if (resultado === 'failed') {
                servicioNotificaciones.mostrarError('El pago no se pudo completar. Podés intentarlo de nuevo con el botón "Pagar".');
            } else {
                servicioNotificaciones.mostrarError('No pudimos verificar el pago. Si ya pagaste, no hace falta hacer nada más: tu paseador lo confirma. Si no, pagale de otra forma y él lo registra.');
            }
            $location.search('payment', null);
        }

        /* ---------- Buscar cuidador ---------- */

        $scope.pestana = 'buscar';
        $scope.mascotas = [];
        $scope.hoy = moment().format('YYYY-MM-DD');
        $scope.busqueda = { ingreso: null, salida: null, detalles: '' };
        $scope.cuidadores = null;
        $scope.buscado = null;
        $scope.buscando = false;

        //Lo que ya habia cargado (por ejemplo, si fue a mirar el perfil de un cuidador): se recupera al volver
        var anterior = $rootScope.borradorHospedaje && $rootScope.borradorHospedaje.idCliente === $scope.idCliente ? $rootScope.borradorHospedaje : null;
        if (anterior) {
            $scope.pestana = anterior.pestana;
            $scope.busqueda = anterior.busqueda;
            $scope.cuidadores = anterior.cuidadores;
            $scope.buscado = anterior.buscado;
        }
        $rootScope.borradorHospedaje = null;

        //Al salir de la pantalla se guarda lo que habia cargado
        $scope.$on('$destroy', function () {
            $rootScope.borradorHospedaje = {
                idCliente: $scope.idCliente,
                pestana: $scope.pestana,
                busqueda: $scope.busqueda,
                cuidadores: $scope.cuidadores,
                buscado: $scope.buscado,
                idsMascotas: $scope.elegidas().map(function (mascota) { return mascota.id; })
            };
        });

        servicioApi.get('/api/pets/getAllByCustomerId/', { params: { customerId: $scope.idCliente } }, function (resultado) {
            $scope.mascotas = resultado.data;
            $scope.mascotas.forEach(function (mascota) {
                mascota.seleccionada = anterior ? anterior.idsMascotas.indexOf(mascota.id) !== -1 : $scope.mascotas.length === 1;
            });
        });

        servicioApi.get('/api/customers/getByUserId', { params: { userId: usuario.id } }, function (resultado) {
            $scope.cliente = resultado.data;
        });

        cargarHospedajes();
        mostrarResultadoPago();
        var temporizador = $interval(cargarHospedajes, segundosEntreActualizaciones * 1000);
        $scope.$on('$destroy', function () { $interval.cancel(temporizador); });

        $scope.elegidas = function () {
            return ($scope.mascotas || []).filter(function (mascota) { return mascota.seleccionada; });
        };

        $scope.cambiarBusqueda = function () {
            $scope.cuidadores = null;
        };

        function fechaDeCampo(valor) {
            return valor ? moment(valor).format('YYYY-MM-DD') : null;
        }

        $scope.noches = function () {
            var ingreso = fechaDeCampo($scope.busqueda.ingreso), salida = fechaDeCampo($scope.busqueda.salida);
            return ingreso && salida ? moment(salida).diff(moment(ingreso), 'days') : 0;
        };

        $scope.buscar = function () {
            var ingreso = fechaDeCampo($scope.busqueda.ingreso), salida = fechaDeCampo($scope.busqueda.salida);
            if ($scope.elegidas().length === 0) {
                servicioNotificaciones.mostrarError('Elegí al menos un perro.');
                return;
            }
            if (!ingreso || !salida) {
                servicioNotificaciones.mostrarError('Elegí la fecha de ingreso y la de salida.');
                return;
            }

            var parametros = { customerId: $scope.idCliente, checkIn: ingreso, checkOut: salida, dogs: $scope.elegidas().length };
            if ($scope.cliente && $scope.cliente.latitude && $scope.cliente.longitude) {
                parametros.latitude = $scope.cliente.latitude;
                parametros.longitude = $scope.cliente.longitude;
            }

            $scope.buscando = true;
            servicioApi.get('/api/stays/search', { params: parametros }, function (resultado) {
                $scope.buscando = false;
                $scope.cuidadores = resultado.data;
                $scope.cuidadores.forEach(function (cuidador) { cuidador.pago = 'Cash'; });
                $scope.buscado = { ingreso: ingreso, salida: salida, perros: parametros.dogs };
            }, function (error) {
                $scope.buscando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo buscar.'));
            });
        };

        $scope.solicitar = function (cuidador) {
            servicioConfirmacion.preguntar({
                title: '¿Pedir el hospedaje?',
                text: $scope.textoPerros(cuidador.dogs) + ' con ' + cuidador.name + ' del ' + $scope.fecha($scope.buscado.ingreso) + ' al ' + $scope.fecha($scope.buscado.salida) +
                      ' (' + cuidador.nights + (cuidador.nights === 1 ? ' noche' : ' noches') + '): ' + $scope.dinero(cuidador.total) + ' en total, ' +
                      (cuidador.pago === 'MercadoPago' ? 'a pagar por Mercado Pago' : 'a pagar en efectivo') + ' al terminar. El cuidador tiene que confirmarlo.',
                confirmLabel: 'Pedir hospedaje',
                cancelLabel: 'Volver'
            }).then(function () {
                $scope.trabajando = true;
                servicioApi.post('/api/stays/request', {
                    customerId: $scope.idCliente,
                    walkerId: cuidador.walkerId,
                    petIds: $scope.elegidas().map(function (mascota) { return mascota.id; }),
                    checkIn: $scope.buscado.ingreso,
                    checkOut: $scope.buscado.salida,
                    details: $scope.busqueda.detalles,
                    paymentMethod: cuidador.pago
                }, function () {
                    $scope.trabajando = false;
                    servicioNotificaciones.mostrarExito('Pedido enviado. Cuando ' + cuidador.name + ' lo confirme, van a poder coordinar la entrega por el chat.');
                    $scope.cuidadores = null;
                    $scope.pestana = 'mis';
                    cargarHospedajes();
                }, function (error) {
                    $scope.trabajando = false;
                    servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo pedir el hospedaje.'));
                    $scope.buscar();
                });
            });
        };
    }

})(angular.module('walkyDoggy'));

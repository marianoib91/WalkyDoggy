(function (app) {
    'use strict';

    app.controller('inicioCtrl', inicioCtrl);

    inicioCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', '$rootScope'];

    function inicioCtrl($scope, servicioApi, servicioNotificaciones, servicioConfirmacion, $rootScope) {
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

        $scope.userData.mostrarDatosUsuario();
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.idPaseador = $rootScope.repository.loggedUser.walkerId;
        $scope.idCliente = $rootScope.repository.loggedUser.customerId;

        $scope.trabajando = false;

        //Paseador: sus reservas segun en que punto del circuito estan
        $scope.pendientes = [];
        $scope.porFinalizar = [];
        $scope.proximos = [];
        $scope.porCobrar = [];
        $scope.cobrados = [];
        $scope.reservasCargadas = false;

        iniciar();

        function iniciar() {
            //Paseador
            if ($scope.roleId == '3') {
                cargarReservas();

                //Se consulta el perfil para avisar si todavia no vinculo su cuenta de Mercado Pago
                var idUsuario = $rootScope.repository.loggedUser.id;
                servicioApi.get('/api/walkers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
                    $scope.perfilPaseador = resultado.data;
                });
            }
        }

        /* ---------- Paseador: solicitudes, paseos y cobros ---------- */

        //Montos al estilo argentino (4321.5 -> 4.321,50)
        function formatearMonto(monto) {
            return Number(monto).toLocaleString('es-AR', { minimumFractionDigits: monto % 1 ? 2 : 0, maximumFractionDigits: 2 });
        }

        function inicioDe(reserva) {
            return moment(reserva.date).hour(Number(reserva.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function cargarReservas() {
            servicioApi.get('/api/walks/getBookingsForWalker', { params: { walkerId: $scope.idPaseador } }, function (resultado) {
                var ahora = new Date();
                var pendientes = [], porFinalizar = [], proximos = [], porCobrar = [], cobrados = [];

                angular.forEach(resultado.data, function (reserva) {
                    if (reserva.status === 'Pending') {
                        pendientes.push(reserva);
                    } else if (reserva.status === 'Confirmed') {
                        if (reserva.receivedAt) {
                            cobrados.push(reserva);
                        } else if (reserva.finishedAt) {
                            porCobrar.push(reserva);
                        } else if (inicioDe(reserva) <= ahora) {
                            porFinalizar.push(reserva);
                        } else {
                            proximos.push(reserva);
                        }
                    }
                });

                $scope.pendientes = pendientes;
                $scope.porFinalizar = porFinalizar;
                $scope.proximos = proximos;
                $scope.porCobrar = porCobrar;
                $scope.cobrados = cobrados.sort(function (a, b) { return new Date(b.receivedAt) - new Date(a.receivedAt); });
                $scope.reservasCargadas = true;
            });
        }

        $scope.textoFecha = function (reserva) {
            var fecha = moment(reserva.date);
            return nombresDias[fecha.day()] + ' ' + fecha.format('DD/MM/YYYY');
        };

        $scope.textoHorario = function (reserva) {
            return reserva.timeFrom + ' a ' + (Number(reserva.timeFrom.split(':')[0]) + 1) + ':00';
        };

        $scope.nombresMascotas = function (reserva) {
            return reserva.pets.map(function (mascota) { return mascota.name; }).join(', ');
        };

        $scope.textoPago = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return reserva.paymentStatus === 'Received' ? 'Efectivo · cobrado' : 'Efectivo · te paga en mano al terminar';
            }

            if (reserva.paymentStatus === 'Received') {
                return 'Mercado Pago · cobrado';
            }
            if (reserva.paymentStatus === 'Paid') {
                return 'Mercado Pago · el cliente ya pagó, el dinero está en tu cuenta';
            }
            return reserva.finishedAt
                ? 'Mercado Pago · esperando que el cliente pague'
                : 'Mercado Pago · el cliente te paga online cuando termines el paseo';
        };

        $scope.textoEstadoCobro = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return 'Cobrar en efectivo';
            }
            return reserva.paymentStatus === 'Paid' ? 'Pagado' : 'Esperando el pago';
        };

        //En efectivo se confirma directamente; con Mercado Pago, cuando el cliente ya pago
        $scope.puedeRecibir = function (reserva) {
            return reserva.paymentMethod !== 'MercadoPago' || reserva.paymentStatus === 'Paid';
        };

        $scope.confirmar = function (reserva) {
            enviarAccion('/api/walks/confirm', reserva, 'Confirmaste el paseo. El cliente ya lo ve como confirmado.');
        };

        $scope.rechazar = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Rechazar la solicitud?',
                text: 'La solicitud de ' + reserva.customerFullName + ' para ' + $scope.nombresMascotas(reserva) + ' se va a cancelar.',
                confirmLabel: 'Rechazar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/walks/cancel', reserva, 'Rechazaste la solicitud.');
            });
        };

        $scope.cancelarConfirmado = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar el paseo confirmado?',
                text: 'El paseo de ' + $scope.nombresMascotas(reserva) + ' del ' + $scope.textoFecha(reserva) + ' a las ' + reserva.timeFrom + ' se va a cancelar y el cliente lo va a ver como cancelado.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/walks/cancel', reserva, 'El paseo se canceló.');
            });
        };

        $scope.finalizar = function (reserva) {
            var notaDePago = reserva.paymentMethod === 'MercadoPago'
                ? 'El cliente va a ver el botón para pagarte por Mercado Pago.'
                : 'El cliente te paga en efectivo.';

            servicioConfirmacion.preguntar({
                title: '¿Terminaste el paseo?',
                text: 'Confirmá que ya devolviste a ' + $scope.nombresMascotas(reserva) + '. ' + notaDePago,
                confirmLabel: 'Sí, terminé',
                cancelLabel: 'Volver'
            }).then(function () {
                enviarAccion('/api/walks/finish', reserva, 'Diste el paseo por finalizado. Ahora el cliente puede pagarte.');
            });
        };

        $scope.recibir = function (reserva) {
            var texto = reserva.paymentMethod === 'MercadoPago'
                ? 'Confirmá que el pago de $' + formatearMonto(reserva.total) + ' de ' + reserva.customerFullName + ' por Mercado Pago ya figura en tu cuenta.'
                : 'Confirmá que ' + reserva.customerFullName + ' te pagó $' + formatearMonto(reserva.total) + ' en efectivo.';

            servicioConfirmacion.preguntar({
                title: '¿Recibiste el pago?',
                text: texto,
                confirmLabel: 'Sí, lo recibí',
                cancelLabel: 'Volver'
            }).then(function () {
                enviarAccion('/api/walks/receive', reserva, 'Listo, el paseo quedó cobrado y cerrado.');
            });
        };

        function enviarAccion(url, reserva, mensajeDeExito) {
            var accion = { bookingKey: reserva.bookingKey, actor: 'Walker', actorId: $scope.idPaseador };

            $scope.trabajando = true;
            servicioApi.post(url, accion, function () {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarExito(mensajeDeExito);
                cargarReservas();
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo completar la acción.');
                cargarReservas();
            });
        }
    }

})(angular.module('walkyDoggy'));

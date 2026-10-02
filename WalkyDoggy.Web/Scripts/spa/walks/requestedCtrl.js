(function (app) {
    'use strict';

    app.controller('paseosSolicitadosCtrl', paseosSolicitadosCtrl);

    paseosSolicitadosCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioValoraciones', '$rootScope', '$location'];

    function paseosSolicitadosCtrl($scope, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioValoraciones, $rootScope, $location) {
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var idCliente = $rootScope.repository.loggedUser.customerId;

        $scope.cargado = false;
        $scope.porPagar = [];
        $scope.proximos = [];
        $scope.historial = [];
        $scope.trabajando = false;

        iniciar();
        mostrarResultadoPago();

        function iniciar() {
            servicioApi.get('/api/walks/getBookingsForCustomer', { params: { customerId: idCliente } }, function (resultado) {
                var porPagar = [];
                var proximos = [];
                var historial = [];

                angular.forEach(resultado.data, function (reserva) {
                    reserva.start = inicioDe(reserva);

                    if (reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending') {
                        //El paseador ya termino el paseo y falta pagarlo
                        porPagar.push(reserva);
                    } else if (reserva.status !== 'Cancelled' && !reserva.finishedAt) {
                        proximos.push(reserva);
                    } else {
                        historial.push(reserva);
                    }
                });

                //Lo que hay que pagar y lo proximo, del mas cercano al mas lejano; el historial ya viene del mas reciente al mas antiguo
                $scope.porPagar = porPagar.sort(function (a, b) { return a.start - b.start; });
                $scope.proximos = proximos.sort(function (a, b) { return a.start - b.start; });
                $scope.historial = historial;
                $scope.cargado = true;
            });
        }

        function inicioDe(reserva) {
            return moment(reserva.date).hour(Number(reserva.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function mensajeDeError(error, porDefecto) {
            return error.data && error.data[0] ? error.data[0] : porDefecto;
        }

        //Mercado Pago devuelve al cliente a "Paseos solicitados?payment=..." cuando termina de pagar
        function mostrarResultadoPago() {
            var resultado = $location.search().payment;
            if (!resultado) {
                return;
            }

            if (resultado === 'approved') {
                servicioNotificaciones.mostrarExito('¡Pago recibido! El dinero ya está en la cuenta de tu paseador.');
            } else if (resultado === 'pending') {
                servicioNotificaciones.mostrarError('Tu pago todavía no se acreditó. Cuando se confirme lo vas a ver acá; si ya pagaste, tocá "Ya pagué, verificar".');
            } else if (resultado === 'failed') {
                servicioNotificaciones.mostrarError('El pago no se pudo completar. Podés intentarlo de nuevo con el botón "Pagar".');
            } else {
                servicioNotificaciones.mostrarError('No pudimos verificar el pago. Si ya pagaste, tocá "Ya pagué, verificar".');
            }

            $location.search({}).replace();
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

        $scope.textoEstado = function (reserva) {
            if (reserva.status === 'Pending') {
                return 'Esperando confirmación';
            }
            if (reserva.status === 'Confirmed') {
                if (reserva.paymentStatus === 'Received') {
                    return 'Pagado';
                }
                if (reserva.finishedAt) {
                    return reserva.paymentStatus === 'Paid' ? 'Pagado, falta que lo confirme el paseador' : 'Para pagar';
                }
                return reserva.start <= new Date() ? 'En curso' : 'Confirmado';
            }
            if (reserva.cancelledBy === 'Walker') {
                return 'Cancelado por el paseador';
            }
            if (reserva.cancelledBy === 'Customer') {
                return 'Cancelado por vos';
            }
            return 'Sin respuesta del paseador';
        };

        $scope.claseEstado = function (reserva) {
            if (reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending') {
                return 'wd-status-pending';
            }
            return 'wd-status-' + reserva.status.toLowerCase();
        };

        $scope.textoPago = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return reserva.paymentStatus === 'Received' ? 'Efectivo · pagado' : 'Efectivo · le pagás en mano al paseador';
            }

            if (reserva.paymentStatus === 'Received') {
                return 'Mercado Pago · pagado';
            }
            if (reserva.paymentStatus === 'Paid') {
                return 'Mercado Pago · pagado';
            }
            return reserva.finishedAt
                ? 'Mercado Pago · pendiente de pago'
                : 'Mercado Pago · pagás online cuando el paseador termine el paseo';
        };

        /* ---------- Cancelar ---------- */

        $scope.puedeCancelar = function (reserva) {
            return reserva.status !== 'Cancelled' && reserva.start > new Date();
        };

        $scope.cancelar = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar este paseo?',
                text: 'El paseo con ' + reserva.walkerName + ' del ' + $scope.textoFecha(reserva) + ' a las ' + reserva.timeFrom + ' se va a cancelar.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                var accion = { bookingKey: reserva.bookingKey, actor: 'Customer', actorId: idCliente };
                servicioApi.post('/api/walks/cancel', accion, function () {
                    servicioNotificaciones.mostrarExito('El paseo se canceló.');
                    iniciar();
                }, function (error) {
                    servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo cancelar el paseo.'));
                    iniciar();
                });
            });
        };

        /* ---------- Valorar al paseador ---------- */

        //Se puede valorar un paseo que el paseador ya dio por finalizado y que todavia no se valoro
        $scope.puedeValorar = function (reserva) {
            return reserva.status === 'Confirmed' && !!reserva.finishedAt && !reserva.ratingStars;
        };

        $scope.valorar = function (reserva) {
            servicioValoraciones.preguntar({ walkerName: reserva.walkerName, petNames: $scope.nombresMascotas(reserva) }).then(function (valoracion) {
                var pedido = { bookingKey: reserva.bookingKey, customerId: idCliente, stars: valoracion.stars, comment: valoracion.comment };
                servicioApi.post('/api/ratings/rate', pedido, function () {
                    servicioNotificaciones.mostrarExito('¡Gracias por tu valoración!');
                    iniciar();
                }, function (error) {
                    servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo enviar la valoración.'));
                    iniciar();
                });
            });
        };

        /* ---------- Pago con Mercado Pago ---------- */

        $scope.esMercadoPago = function (reserva) {
            return reserva.paymentMethod === 'MercadoPago';
        };

        $scope.pagar = function (reserva) {
            $scope.trabajando = true;

            //El servidor crea el pago en Mercado Pago (a nombre del paseador) y devuelve su direccion: ahi el cliente paga y despues vuelve a la app
            servicioApi.post('/api/payments/checkout', { bookingKey: reserva.bookingKey, customerId: idCliente }, function (resultado) {
                window.location.href = resultado.data.url;
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo iniciar el pago.'));
            });
        };

        //Por si el cliente pago pero no volvio a la app despues de pagar
        $scope.verificarPago = function (reserva) {
            $scope.trabajando = true;

            servicioApi.post('/api/payments/sync', { bookingKey: reserva.bookingKey, customerId: idCliente }, function (resultado) {
                $scope.trabajando = false;
                if (resultado.data.result === 'approved') {
                    servicioNotificaciones.mostrarExito('Encontramos tu pago. El dinero ya está en la cuenta de tu paseador.');
                } else {
                    servicioNotificaciones.mostrarError('Todavía no encontramos un pago aprobado para este paseo.');
                }
                iniciar();
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo verificar el pago.'));
            });
        };
    }

})(angular.module('walkyDoggy'));

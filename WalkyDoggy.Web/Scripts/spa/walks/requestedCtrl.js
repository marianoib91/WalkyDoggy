(function (app) {
    'use strict';

    app.controller('requestedCtrl', requestedCtrl);

    requestedCtrl.$inject = ['$scope', 'apiService', 'notificationService', 'confirmService', '$rootScope', '$location'];

    function requestedCtrl($scope, apiService, notificationService, confirmService, $rootScope, $location) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var customerId = $rootScope.repository.loggedUser.customerId;

        $scope.loaded = false;
        $scope.upcoming = [];
        $scope.history = [];
        $scope.working = false;

        init();
        showPaymentResult();

        function init() {
            apiService.get('/api/walks/getBookingsForCustomer', { params: { customerId: customerId } }, function (result) {
                var upcoming = [];
                var history = [];

                angular.forEach(result.data, function (booking) {
                    booking.start = startOf(booking);
                    (isUpcoming(booking) ? upcoming : history).push(booking);
                });

                //Los proximos, del mas cercano al mas lejano; el historial ya viene del mas reciente al mas antiguo
                $scope.upcoming = upcoming.sort(function (a, b) { return a.start - b.start; });
                $scope.history = history;
                $scope.loaded = true;
            });
        }

        function startOf(booking) {
            return moment(booking.date).hour(Number(booking.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function isUpcoming(booking) {
            return booking.status !== 'Cancelled' && booking.start > new Date();
        }

        function endOf(booking) {
            return moment(booking.start).add(1, 'hours').toDate();
        }

        function errorMessage(error, fallback) {
            return error.data && error.data[0] ? error.data[0] : fallback;
        }

        //Mercado Pago devuelve al cliente a "Paseos solicitados?payment=..." cuando termina de pagar
        function showPaymentResult() {
            var result = $location.search().payment;
            if (!result) {
                return;
            }

            if (result === 'approved') {
                notificationService.displaySuccess('¡Pago recibido! WalkyDoggy lo retiene y se lo libera al paseador cuando el paseo se hace.');
            } else if (result === 'pending') {
                notificationService.displayError('Tu pago todavía no se acreditó. Cuando se confirme lo vas a ver acá; si ya pagaste, tocá "Ya pagué, verificar".');
            } else if (result === 'failed') {
                notificationService.displayError('El pago no se pudo completar. Podés intentarlo de nuevo con el botón "Pagar".');
            } else if (result === 'refunded') {
                notificationService.displayError('Este paseo ya estaba cancelado, por eso el pago se te devolvió.');
            } else {
                notificationService.displayError('No pudimos verificar el pago. Si ya pagaste, tocá "Ya pagué, verificar".');
            }

            $location.search({}).replace();
        }

        $scope.dateText = function (booking) {
            var date = moment(booking.date);
            return dayNames[date.day()] + ' ' + date.format('DD/MM/YYYY');
        };

        $scope.timeText = function (booking) {
            return booking.timeFrom + ' a ' + (Number(booking.timeFrom.split(':')[0]) + 1) + ':00';
        };

        $scope.petNames = function (booking) {
            return booking.pets.map(function (pet) { return pet.name; }).join(', ');
        };

        $scope.statusText = function (booking) {
            if (booking.status === 'Pending') {
                return 'Esperando confirmación';
            }
            if (booking.status === 'Confirmed') {
                return $scope.canPay(booking) ? 'Confirmado, falta pagar' : 'Confirmado';
            }
            if (booking.cancelledBy === 'Walker') {
                return 'Cancelado por el paseador';
            }
            if (booking.cancelledBy === 'Customer') {
                return 'Cancelado por vos';
            }
            if (booking.cancelledBy === 'NoPayment') {
                return 'Cancelado por falta de pago';
            }
            return 'Sin respuesta del paseador';
        };

        $scope.statusClass = function (booking) {
            return 'wd-status-' + booking.status.toLowerCase();
        };

        $scope.paymentText = function (booking) {
            if (booking.paymentMethod !== 'MercadoPago') {
                return 'Efectivo';
            }

            switch (booking.paymentStatus) {
                case 'Held': return 'Mercado Pago · pagado, retenido hasta que se haga el paseo';
                case 'Released': return 'Mercado Pago · pagado';
                case 'Refunded': return 'Mercado Pago · reembolsado';
                case 'Disputed': return 'Mercado Pago · pagado, tu reclamo está en revisión';
            }

            if (booking.status === 'Pending') {
                return 'Mercado Pago · se paga cuando el paseador confirme';
            }
            return booking.status === 'Confirmed' ? 'Mercado Pago · pendiente de pago' : 'Mercado Pago';
        };

        /* ---------- Cancelar ---------- */

        $scope.canCancel = function (booking) {
            return isUpcoming(booking);
        };

        $scope.cancel = function (booking) {
            var refunds = booking.paymentStatus === 'Held';

            confirmService.ask({
                title: '¿Cancelar este paseo?',
                text: 'El paseo con ' + booking.walkerName + ' del ' + $scope.dateText(booking) + ' a las ' + booking.timeFrom + ' se va a cancelar.' +
                      (refunds ? ' Se te devuelve el pago completo.' : ''),
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                var action = { bookingKey: booking.bookingKey, actor: 'Customer', actorId: customerId };
                apiService.post('/api/walks/cancel', action, function () {
                    notificationService.displaySuccess(refunds ? 'El paseo se canceló y se te devolvió el pago.' : 'El paseo se canceló.');
                    init();
                }, function (error) {
                    notificationService.displayError(errorMessage(error, 'No se pudo cancelar el paseo.'));
                    init();
                });
            });
        };

        /* ---------- Pago con Mercado Pago ---------- */

        //Se puede pagar cuando el paseador confirmo y todavia no empezo el paseo
        $scope.canPay = function (booking) {
            return booking.paymentMethod === 'MercadoPago' && booking.status === 'Confirmed' &&
                   booking.paymentStatus === 'Pending' && booking.start > new Date();
        };

        $scope.pay = function (booking) {
            $scope.working = true;

            //El servidor crea el pago en Mercado Pago y devuelve su direccion: ahi el cliente paga y despues vuelve a la app
            apiService.post('/api/payments/checkout', { bookingKey: booking.bookingKey, customerId: customerId }, function (result) {
                window.location.href = result.data.url;
            }, function (error) {
                $scope.working = false;
                notificationService.displayError(errorMessage(error, 'No se pudo iniciar el pago.'));
            });
        };

        //Por si el cliente pago pero cerro la pagina antes de volver a la app
        $scope.verifyPayment = function (booking) {
            $scope.working = true;

            apiService.post('/api/payments/sync', { bookingKey: booking.bookingKey, customerId: customerId }, function (result) {
                $scope.working = false;
                if (result.data.result === 'approved') {
                    notificationService.displaySuccess('Encontramos tu pago. Quedó retenido hasta que se haga el paseo.');
                } else {
                    notificationService.displayError('Todavía no encontramos un pago aprobado para este paseo.');
                }
                init();
            }, function (error) {
                $scope.working = false;
                notificationService.displayError(errorMessage(error, 'No se pudo verificar el pago.'));
            });
        };

        //Con el pago retenido: cuando el paseo termino el cliente confirma que salio bien, o reclama si hubo un problema
        $scope.canRelease = function (booking) {
            return booking.paymentMethod === 'MercadoPago' && booking.status === 'Confirmed' &&
                   booking.paymentStatus === 'Held' && endOf(booking) <= new Date();
        };

        $scope.canDispute = function (booking) {
            return booking.paymentMethod === 'MercadoPago' && booking.status === 'Confirmed' &&
                   booking.paymentStatus === 'Held' && booking.start <= new Date();
        };

        $scope.release = function (booking) {
            confirmService.ask({
                title: '¿El paseo salió bien?',
                text: 'Al confirmarlo, WalkyDoggy le libera el pago a ' + booking.walkerName + '. Si no hacés nada, se libera solo a las 24 horas de terminado el paseo.',
                confirmLabel: 'Sí, salió bien',
                cancelLabel: 'Volver'
            }).then(function () {
                apiService.post('/api/payments/release', { bookingKey: booking.bookingKey, customerId: customerId }, function () {
                    notificationService.displaySuccess('Listo, le liberamos el pago al paseador. ¡Gracias!');
                    init();
                }, function (error) {
                    notificationService.displayError(errorMessage(error, 'No se pudo liberar el pago.'));
                    init();
                });
            });
        };

        $scope.dispute = function (booking) {
            confirmService.ask({
                title: 'Reportar un problema',
                text: 'El pago queda retenido y no se le libera al paseador mientras revisamos lo que pasó.',
                input: { label: '¿Qué pasó con el paseo?', placeholder: 'Por ejemplo: el paseador no se presentó.' },
                confirmLabel: 'Enviar reclamo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function (reason) {
                apiService.post('/api/payments/dispute', { bookingKey: booking.bookingKey, customerId: customerId, reason: reason }, function () {
                    notificationService.displaySuccess('Recibimos tu reclamo. El pago queda retenido mientras lo revisamos.');
                    init();
                }, function (error) {
                    notificationService.displayError(errorMessage(error, 'No se pudo enviar el reclamo.'));
                    init();
                });
            });
        };
    }

})(angular.module('walkyDoggy'));

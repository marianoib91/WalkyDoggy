(function (app) {
    'use strict';

    app.controller('requestedCtrl', requestedCtrl);

    requestedCtrl.$inject = ['$scope', 'apiService', 'notificationService', 'confirmService', 'ratingService', '$rootScope', '$location'];

    function requestedCtrl($scope, apiService, notificationService, confirmService, ratingService, $rootScope, $location) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var customerId = $rootScope.repository.loggedUser.customerId;

        $scope.loaded = false;
        $scope.toPay = [];
        $scope.upcoming = [];
        $scope.history = [];
        $scope.working = false;

        init();
        showPaymentResult();

        function init() {
            apiService.get('/api/walks/getBookingsForCustomer', { params: { customerId: customerId } }, function (result) {
                var toPay = [];
                var upcoming = [];
                var history = [];

                angular.forEach(result.data, function (booking) {
                    booking.start = startOf(booking);

                    if (booking.status === 'Confirmed' && booking.finishedAt && booking.paymentStatus === 'Pending') {
                        //El paseador ya termino el paseo y falta pagarlo
                        toPay.push(booking);
                    } else if (booking.status !== 'Cancelled' && !booking.finishedAt) {
                        upcoming.push(booking);
                    } else {
                        history.push(booking);
                    }
                });

                //Lo que hay que pagar y lo proximo, del mas cercano al mas lejano; el historial ya viene del mas reciente al mas antiguo
                $scope.toPay = toPay.sort(function (a, b) { return a.start - b.start; });
                $scope.upcoming = upcoming.sort(function (a, b) { return a.start - b.start; });
                $scope.history = history;
                $scope.loaded = true;
            });
        }

        function startOf(booking) {
            return moment(booking.date).hour(Number(booking.timeFrom.split(':')[0])).minute(0).second(0).toDate();
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
                notificationService.displaySuccess('¡Pago recibido! El dinero ya está en la cuenta de tu paseador.');
            } else if (result === 'pending') {
                notificationService.displayError('Tu pago todavía no se acreditó. Cuando se confirme lo vas a ver acá; si ya pagaste, tocá "Ya pagué, verificar".');
            } else if (result === 'failed') {
                notificationService.displayError('El pago no se pudo completar. Podés intentarlo de nuevo con el botón "Pagar".');
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
                if (booking.paymentStatus === 'Received') {
                    return 'Pagado';
                }
                if (booking.finishedAt) {
                    return booking.paymentStatus === 'Paid' ? 'Pagado, falta que lo confirme el paseador' : 'Para pagar';
                }
                return booking.start <= new Date() ? 'En curso' : 'Confirmado';
            }
            if (booking.cancelledBy === 'Walker') {
                return 'Cancelado por el paseador';
            }
            if (booking.cancelledBy === 'Customer') {
                return 'Cancelado por vos';
            }
            return 'Sin respuesta del paseador';
        };

        $scope.statusClass = function (booking) {
            if (booking.status === 'Confirmed' && booking.finishedAt && booking.paymentStatus === 'Pending') {
                return 'wd-status-pending';
            }
            return 'wd-status-' + booking.status.toLowerCase();
        };

        $scope.paymentText = function (booking) {
            if (booking.paymentMethod !== 'MercadoPago') {
                return booking.paymentStatus === 'Received' ? 'Efectivo · pagado' : 'Efectivo · le pagás en mano al paseador';
            }

            if (booking.paymentStatus === 'Received') {
                return 'Mercado Pago · pagado';
            }
            if (booking.paymentStatus === 'Paid') {
                return 'Mercado Pago · pagado';
            }
            return booking.finishedAt
                ? 'Mercado Pago · pendiente de pago'
                : 'Mercado Pago · pagás online cuando el paseador termine el paseo';
        };

        /* ---------- Cancelar ---------- */

        $scope.canCancel = function (booking) {
            return booking.status !== 'Cancelled' && booking.start > new Date();
        };

        $scope.cancel = function (booking) {
            confirmService.ask({
                title: '¿Cancelar este paseo?',
                text: 'El paseo con ' + booking.walkerName + ' del ' + $scope.dateText(booking) + ' a las ' + booking.timeFrom + ' se va a cancelar.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                var action = { bookingKey: booking.bookingKey, actor: 'Customer', actorId: customerId };
                apiService.post('/api/walks/cancel', action, function () {
                    notificationService.displaySuccess('El paseo se canceló.');
                    init();
                }, function (error) {
                    notificationService.displayError(errorMessage(error, 'No se pudo cancelar el paseo.'));
                    init();
                });
            });
        };

        /* ---------- Valorar al paseador ---------- */

        //Se puede valorar un paseo que el paseador ya dio por finalizado y que todavia no se valoro
        $scope.canRate = function (booking) {
            return booking.status === 'Confirmed' && !!booking.finishedAt && !booking.ratingStars;
        };

        $scope.rate = function (booking) {
            ratingService.ask({ walkerName: booking.walkerName, petNames: $scope.petNames(booking) }).then(function (rating) {
                var request = { bookingKey: booking.bookingKey, customerId: customerId, stars: rating.stars, comment: rating.comment };
                apiService.post('/api/ratings/rate', request, function () {
                    notificationService.displaySuccess('¡Gracias por tu valoración!');
                    init();
                }, function (error) {
                    notificationService.displayError(errorMessage(error, 'No se pudo enviar la valoración.'));
                    init();
                });
            });
        };

        /* ---------- Pago con Mercado Pago ---------- */

        $scope.isMercadoPago = function (booking) {
            return booking.paymentMethod === 'MercadoPago';
        };

        $scope.pay = function (booking) {
            $scope.working = true;

            //El servidor crea el pago en Mercado Pago (a nombre del paseador) y devuelve su direccion: ahi el cliente paga y despues vuelve a la app
            apiService.post('/api/payments/checkout', { bookingKey: booking.bookingKey, customerId: customerId }, function (result) {
                window.location.href = result.data.url;
            }, function (error) {
                $scope.working = false;
                notificationService.displayError(errorMessage(error, 'No se pudo iniciar el pago.'));
            });
        };

        //Por si el cliente pago pero no volvio a la app despues de pagar
        $scope.verifyPayment = function (booking) {
            $scope.working = true;

            apiService.post('/api/payments/sync', { bookingKey: booking.bookingKey, customerId: customerId }, function (result) {
                $scope.working = false;
                if (result.data.result === 'approved') {
                    notificationService.displaySuccess('Encontramos tu pago. El dinero ya está en la cuenta de tu paseador.');
                } else {
                    notificationService.displayError('Todavía no encontramos un pago aprobado para este paseo.');
                }
                init();
            }, function (error) {
                $scope.working = false;
                notificationService.displayError(errorMessage(error, 'No se pudo verificar el pago.'));
            });
        };
    }

})(angular.module('walkyDoggy'));

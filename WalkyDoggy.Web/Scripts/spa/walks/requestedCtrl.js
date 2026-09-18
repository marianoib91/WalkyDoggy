(function (app) {
    'use strict';

    app.controller('requestedCtrl', requestedCtrl);

    requestedCtrl.$inject = ['$scope', 'apiService', 'notificationService', 'confirmService', '$rootScope'];

    function requestedCtrl($scope, apiService, notificationService, confirmService, $rootScope) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var customerId = $rootScope.repository.loggedUser.customerId;

        $scope.loaded = false;
        $scope.upcoming = [];
        $scope.history = [];

        init();

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
                return 'Confirmado';
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
            return 'wd-status-' + booking.status.toLowerCase();
        };

        $scope.paymentText = function (booking) {
            var method = booking.paymentMethod === 'MercadoPago' ? 'Mercado Pago' : 'Efectivo';
            return method + (booking.paymentStatus === 'Paid' ? ' (pagado)' : '');
        };

        $scope.canCancel = function (booking) {
            return isUpcoming(booking);
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
                    notificationService.displayError(error.data && error.data[0] ? error.data[0] : 'No se pudo cancelar el paseo.');
                    init();
                });
            });
        };
    }

})(angular.module('walkyDoggy'));

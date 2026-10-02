(function (app) {
    'use strict';

    app.controller('indexCtrl', indexCtrl);

    indexCtrl.$inject = ['$scope', 'apiService', 'notificationService', 'confirmService', '$rootScope'];

    function indexCtrl($scope, apiService, notificationService, confirmService, $rootScope) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

        $scope.userData.displayUserInfo();
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.walkerId = $rootScope.repository.loggedUser.walkerId;
        $scope.customerId = $rootScope.repository.loggedUser.customerId;

        $scope.walkers = {};
        $scope.hasDistances = false;

        $scope.pending = [];
        $scope.confirmed = [];
        $scope.loadedBookings = false;

        init();

        function init() {
            //Cliente
            if ($scope.roleId == '2') {
                //Los paseadores llegan ordenados por cercania al domicilio del cliente
                apiService.get('/api/walkers/getAllOrderedByDistance', { params: { customerId: $scope.customerId } }, onLoadWalkersCompleted);
            }

            //Paseador
            if ($scope.roleId == '3') {
                loadBookings();
                loadBalance();
            }
        }

        function onLoadWalkersCompleted(result) {
            $scope.walkers = result.data;
            $scope.hasDistances = result.data.some(function (walker) {
                return walker.distanceKm !== null && walker.distanceKm !== undefined;
            });
        }

        /* ---------- Paseador: solicitudes y paseos confirmados ---------- */

        function loadBookings() {
            apiService.get('/api/walks/getBookingsForWalker', { params: { walkerId: $scope.walkerId } }, function (result) {
                $scope.pending = result.data.filter(function (booking) { return booking.status === 'Pending'; });
                $scope.confirmed = result.data.filter(function (booking) { return booking.status === 'Confirmed'; });
                $scope.loadedBookings = true;
            });
        }

        //Lo que WalkyDoggy tiene cobrado con Mercado Pago a nombre del paseador: retenido, a liquidar y en revision
        function loadBalance() {
            apiService.get('/api/payments/walkerBalance', { params: { walkerId: $scope.walkerId } }, function (result) {
                $scope.balance = result.data;
                $scope.balanceLoaded = true;
                $scope.hasBalance = result.data.retained + result.data.toSettle + result.data.inReview > 0;
            });
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

        $scope.paymentText = function (booking) {
            if (booking.paymentMethod !== 'MercadoPago') {
                return 'Efectivo';
            }

            switch (booking.paymentStatus) {
                case 'Held': return 'Mercado Pago · pagado, WalkyDoggy lo retiene hasta que se haga el paseo';
                case 'Released': return 'Mercado Pago · pagado, WalkyDoggy te lo liquida';
                case 'Disputed': return 'Mercado Pago · el cliente reclamó, está en revisión';
            }

            return booking.status === 'Pending'
                ? 'Mercado Pago · el cliente paga cuando confirmes'
                : 'Mercado Pago · esperando el pago del cliente';
        };

        $scope.confirm = function (booking) {
            send('/api/walks/confirm', booking, 'Confirmaste el paseo. El cliente ya lo ve como confirmado.');
        };

        $scope.reject = function (booking) {
            confirmService.ask({
                title: '¿Rechazar la solicitud?',
                text: 'La solicitud de ' + booking.customerFullName + ' para ' + $scope.petNames(booking) + ' se va a cancelar.',
                confirmLabel: 'Rechazar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                send('/api/walks/cancel', booking, 'Rechazaste la solicitud.');
            });
        };

        $scope.cancelConfirmed = function (booking) {
            confirmService.ask({
                title: '¿Cancelar el paseo confirmado?',
                text: 'El paseo de ' + $scope.petNames(booking) + ' del ' + $scope.dateText(booking) + ' a las ' + booking.timeFrom + ' se va a cancelar y el cliente lo va a ver como cancelado.' +
                      (booking.paymentStatus === 'Held' ? ' Como ya pagó, se le devuelve el dinero.' : ''),
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                send('/api/walks/cancel', booking, 'El paseo se canceló.');
            });
        };

        function send(url, booking, successMessage) {
            var action = { bookingKey: booking.bookingKey, actor: 'Walker', actorId: $scope.walkerId };

            apiService.post(url, action, function () {
                notificationService.displaySuccess(successMessage);
                loadBookings();
            }, function (error) {
                notificationService.displayError(error.data && error.data[0] ? error.data[0] : 'No se pudo completar la acción.');
                loadBookings();
            });
        }
    }

})(angular.module('walkyDoggy'));

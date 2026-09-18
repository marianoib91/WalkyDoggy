(function (app) {
    'use strict';

    app.controller('step2Ctrl', step2Ctrl);

    step2Ctrl.$inject = ['$scope', 'apiService', 'notificationService', '$rootScope', '$location'];

    function step2Ctrl($scope, apiService, notificationService, $rootScope, $location) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var draft = $rootScope.walkDraft;

        //Si se llega sin haber elegido dia y horario en el paso 1 se vuelve al listado de paseadores
        if (!draft) {
            $location.search({}).path('/');
            return;
        }

        $scope.draft = draft;
        $scope.walker = null;
        $scope.details = '';
        $scope.saving = false;

        var walkDate = moment(draft.date, 'YYYY-MM-DD');
        $scope.dateText = dayNames[walkDate.day()] + ' ' + walkDate.format('DD/MM/YYYY');
        $scope.timeText = draft.timeFrom + ' a ' + (Number(draft.timeFrom.split(':')[0]) + 1) + ':00';
        $scope.petNames = draft.pets.map(function (pet) { return pet.name; }).join(', ');

        var pickup = draft.pickup;
        var pickupLine = [pickup.streetName, pickup.streetNumber].filter(Boolean).join(' ');
        $scope.pickupText = [pickupLine, pickup.cityName, pickup.provinceName].filter(Boolean).join(', ');
        $scope.pickupIsHome = pickup.isHome;

        apiService.get('/api/walkers/getDetail', { params: { id: draft.walkerId } }, function (result) {
            $scope.walker = result.data;
        });

        $scope.total = function () {
            return $scope.walker ? $scope.walker.amount * draft.pets.length : 0;
        };

        $scope.submit = function () {
            if ($scope.saving) {
                return;
            }
            $scope.saving = true;

            var request = {
                walkerId: draft.walkerId,
                date: draft.date,
                timeFrom: draft.timeFrom,
                details: $scope.details,
                petIds: draft.pets.map(function (pet) { return pet.id; })
            };

            //Si retira en el domicilio no se manda nada: el servidor usa (y guarda) el domicilio del cliente
            if (!pickup.isHome) {
                request.pickupStreetName = pickup.streetName;
                request.pickupStreetNumber = pickup.streetNumber;
                request.pickupCityId = pickup.cityId;
                request.pickupLatitude = pickup.latitude;
                request.pickupLongitude = pickup.longitude;
            }

            apiService.post('/api/walks/register', request, function () {
                notificationService.displaySuccess('Paseo solicitado con éxito.');
                $rootScope.walkDraft = null;
                $location.search({}).path('/');
            }, function (error) {
                $scope.saving = false;
                var message = error.data && error.data[0] ? error.data[0] : 'No se pudo solicitar el paseo. Intente nuevamente.';
                notificationService.displayError(message);
            });
        };
    }

})(angular.module('walkyDoggy'));

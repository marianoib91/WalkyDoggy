(function (app) {
    'use strict';

    app.controller('workDayCtrl', workDayCtrl);

    workDayCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', '$rootScope', '$location','$routeParams'];

    function workDayCtrl($scope, membershipService, notificationService, apiService, $rootScope, $location, $routeParams) {

        $scope.workDayId = $routeParams.id;
        $scope.workDay = {};
        $scope.times = {};

        init();

        function init() {          

            if ($scope.workDayId != 0 || $scope.workDayId != null || $scope.workDay != undefined) {
                var config = {
                    params: {
                        id: $scope.workDayId
                    }
                }
                apiService.get('/api/workDays/getById', config, onLoadWorkDayCompleted);
            }

            // apiService.get('/api/walks/getAll', null, onLoadTimesCompleted);
        }

        function onLoadWorkDayCompleted(response) {
            $scope.workDay = response.data;
        }

        function onLoadTimesCompleted(response) {
            $scope.times = response.data;
        }

        $scope.submit = function () {
            $scope.workDay.walkerId = $rootScope.repository.loggedUser.walkerId;
            apiService.post('/api/workDays/save', $scope.workDay, onSaveWorkDayCompleted,onSaveWorkDayFailed);
        }

        function onSaveWorkDayCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('La jornada laboral se ha guardado exitosamente.');
                $location.path('/work-conditions');
            }
            else {
                notificationService.displayError('No se pudo guardar la jornada laboral. Intente nuevamente');
            }
        }

        function onSaveWorkDayFailed(response) {
            notificationService.displayError(response.data);
        }

    }

})(angular.module('common.core'));
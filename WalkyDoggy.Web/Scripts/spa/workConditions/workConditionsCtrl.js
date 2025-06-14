(function (app) {
    'use strict';

    app.controller('workConditionsCtrl', workConditionsCtrl);

    workConditionsCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', '$rootScope', '$location', 'sweetAlert'];

    function workConditionsCtrl($scope, membershipService, notificationService, apiService, $rootScope, $location, sweetAlert) {
        var walkerId = $rootScope.repository.loggedUser.walkerId;
        $scope.workDays = {};
        $scope.prices = {};
        $scope.walker = {};
        init();

        function init() {
            var config = {
                params: {
                    walkerId: walkerId
                }
            }
            //TODO:Traer todos los registros del walker logueado
            apiService.get('/api/workDays/getAllByWalkerId', config, onLoadWorkDaysCompleted);
            var config = {
                params: {
                    userId: $rootScope.repository.loggedUser.id
                }
            }
            apiService.get('/api/walkers/getByUserId', config, onLoadWalkerCompleted);
        }

        function onLoadWalkerCompleted(response) {
            $scope.walker = response.data;
            apiService.get('/api/prices/getAll', null, onLoadPricesCompleted);
        }

        function onLoadWorkDaysCompleted(response) {
            $scope.workDays = response.data;
        }
        function onLoadPricesCompleted(results) {
            $scope.prices = results.data;
        }

        $scope.edit = function (id) {
            $location.path('/work-days/edit/' + id);
        }

        $scope.delete = function (id) {
            sweetAlert.swal({
                title: "Eliminar jornada laboral",
                text: "¿Desea eliminar la jornada seleccionada?",
                type: "error",
                showCancelButton: true,
                confirmButtonText: "Aceptar",
                cancelButtonText: "Cancelar"
            }).then(function (isConfirm) {
                if (isConfirm) {
                    var r = true;
                }
                if (r == true) {
                    apiService.remove('/api/workdays/' + id, onDeleteWorkDayCompleted);
                }
            });

        }

        $scope.submit = function () {
            apiService.post('/api/walkers/update', $scope.walker, onUpdateWalkerCompleted);
        }

        function onUpdateWalkerCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('Condiciones laborales guardadas con éxito');
                $location.path('/');
            }
            else {
                notificationService.displayError('No se pudieron guardas las condiciones laborales. Intente nuevamente');
            }
        }

        function onDeleteWorkDayCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('La jornada laboral se ha eliminado con éxito');
                var config = {
                    params: {
                        walkerId: walkerId
                    }
                }
                apiService.get('/api/workDays/getAllByWalkerId', config, onLoadWorkDaysCompleted);
            }
            else {
                notificationService.displayError('No se pudo eliminar la jornada laboral. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
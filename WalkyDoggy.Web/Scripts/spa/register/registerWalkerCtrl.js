(function (app) {
    'use strict';

    app.controller('registerWalkerCtrl', registerWalkerCtrl);

    registerWalkerCtrl.$inject = ['$scope', 'apiService', 'membershipService', 'notificationService', '$rootScope', '$location'];

    function registerWalkerCtrl($scope, apiService, membershipService, notificationService, $rootScope, $location) {

        $scope.walker = {};
        $scope.cities = {};
        $scope.provinces = {};
        $scope.prices = {};
        $scope.disableCities = true;

        init();

        function init() {
            apiService.get('/api/provinces/getAll', null, onLoadProvincesCompleted);
            apiService.get('/api/prices/getAll', null, onLoadPricesCompleted);
        }

        $scope.loadCities = function (provinceId) {
            var config = {
                params: {
                    provinceId: provinceId
                }
            }
            apiService.get('/api/cities/getAllByProvinceId/', config, onLoadCitiesCompleted);
        }

        function onLoadPricesCompleted(result) {
            $scope.prices = result.data;
        }

        function onLoadCitiesCompleted(result) {
            $scope.cities = result.data;
            $scope.disableCities = false;
        }

        function onLoadProvincesCompleted(result) {
            $scope.provinces = result.data;
        }

        $scope.register = function register() {
            if ($scope.walker.password == $scope.walker.confirmPassword) {
                apiService.post('/api/walkers/register', $scope.walker, registerCompleted);
            }
            else {
                notificationService.displayError('Las contraseñas no coinciden');
            }
        }

        function registerCompleted(result) {
            if (result.status = 200) {
                membershipService.saveCredentials(result.data, $scope.walker.firstName);
            }
            else {
                notificationService.displayError('No es posible completar la registración. Intente nuevamente.');
            }
        }
    }

})(angular.module('common.core'));
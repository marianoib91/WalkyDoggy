(function (app) {
    'use strict';

    app.controller('registerCustomerCtrl', registerCustomerCtrl);

    registerCustomerCtrl.$inject = ['$scope', 'apiService', 'membershipService', 'notificationService', '$rootScope', '$location'];

    function registerCustomerCtrl($scope, apiService, membershipService, notificationService, $rootScope, $location) {

        $scope.customer = {};
        $scope.cities = {};
        $scope.provinces = {};
        $scope.disableCities = true;

        init();

        function init() {
            apiService.get('/api/provinces/getAll', null, onLoadProvincesCompleted);
        }

        $scope.loadCities = function (provinceId) {
            var config = {
                params: {
                    provinceId: provinceId
                }
            }

            apiService.get('/api/cities/getAllByProvinceId/', config, onLoadCitiesCompleted);
        }

        function onLoadCitiesCompleted(result) {
            $scope.cities = result.data;
            $scope.disableCities = false;
        }

        function onLoadProvincesCompleted(result) {
            $scope.provinces = result.data;
        }

        $scope.register = function () {
            if ($scope.customer.password == $scope.customer.confirmPassword) {
                apiService.post('/api/customers/register', $scope.customer, registerCompleted)
            }
            else {
                notificationService.displayError('Las contraseñas no coinciden');
            }
        }

        function registerCompleted(result) {
            if (result.status == 200) {
                membershipService.saveCredentials(result.data, $scope.customer.firstName);
            }
            else {
                notificationService.displayError('No es posible completar la registración. Intente nuevamente.');
            }
        }

    }

})(angular.module('common.core'));
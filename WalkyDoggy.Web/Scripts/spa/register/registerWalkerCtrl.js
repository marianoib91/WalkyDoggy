(function (app) {
    'use strict';

    app.controller('registerWalkerCtrl', registerWalkerCtrl);

    registerWalkerCtrl.$inject = ['$scope', 'apiService', 'membershipService', 'notificationService', '$rootScope', '$location'];

    function registerWalkerCtrl($scope, apiService, membershipService, notificationService, $rootScope, $location) {

        $scope.walker = {};
        $scope.prices = {};

        init();

        function init() {
            apiService.get('/api/prices/getAll', null, onLoadPricesCompleted);
        }


        function onLoadPricesCompleted(result) {
            $scope.prices = result.data;
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

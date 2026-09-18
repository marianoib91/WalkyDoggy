(function (app) {
    'use strict';

    app.controller('registerCustomerCtrl', registerCustomerCtrl);

    registerCustomerCtrl.$inject = ['$scope', 'apiService', 'membershipService', 'notificationService', '$rootScope', '$location'];

    function registerCustomerCtrl($scope, apiService, membershipService, notificationService, $rootScope, $location) {

        $scope.customer = {};






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

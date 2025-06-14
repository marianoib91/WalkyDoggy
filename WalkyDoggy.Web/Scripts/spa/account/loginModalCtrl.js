(function (app) {
    'use strict';

    app.controller('loginModalCtrl', loginModalCtrl);

    loginModalCtrl.$inject = ['$scope', 'membershipService', 'notificationService', '$rootScope', '$location', '$modalInstance'];

    function loginModalCtrl($scope, membershipService, notificationService, $rootScope, $location, $modalInstance) {

        $scope.user = {};

        //TODO:Traer el rol en el login para saber a que vista redireccionarlo
        $scope.login = function () {
            membershipService.login($scope.user, loginCompleted);
        }

        function loginCompleted(result) {
            if (result.data.success) {
                $modalInstance.close();
                result.data.password = $scope.user.password;
                membershipService.saveCredentials(result.data, result.data.email);               
            }
            else {
                notificationService.displayError('Imposible iniciar sesión. Intente nuevamente.');
            }
        }
        $scope.closeModal = function () {
            $modalInstance.close();
        }
    }

})(angular.module('common.core'));
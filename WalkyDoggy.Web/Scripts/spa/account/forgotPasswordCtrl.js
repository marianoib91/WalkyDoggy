(function (app) {
    'use strict';

    app.controller('forgotPasswordCtrl', forgotPasswordCtrl);

    forgotPasswordCtrl.$inject = ['$scope', 'membershipService', 'notificationService', '$rootScope', '$location'];

    function forgotPasswordCtrl($scope, membershipService, notificationService, $rootScope, $location) {
        $scope.user = {};
        $scope.recoverPassword = recoverPassword;
        function recoverPassword() {
            notificationService.displaySuccess("Contraseña recuperada con éxito");
            $location.path('/');
        }

    }

})(angular.module('common.core'));
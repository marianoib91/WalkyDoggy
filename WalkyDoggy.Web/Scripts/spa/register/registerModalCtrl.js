(function (app) {
    'use strict';

    app.controller('registroModalCtrl', registroModalCtrl);

    registroModalCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location', '$modalInstance'];

    function registroModalCtrl($scope, servicioMembresia, servicioNotificaciones, $rootScope, $location, $modalInstance) {

        $scope.registrar = function (codigoRedireccion) {
            if (codigoRedireccion == 1) {
                $modalInstance.close();
                $location.path('/register/walker');
            }
            else if (codigoRedireccion == 2) {
                $modalInstance.close();
                $location.path('/register/customer');
            }
        }

        $scope.cerrarModal = function () {
            $modalInstance.close();
        }
    }

})(angular.module('common.core'));
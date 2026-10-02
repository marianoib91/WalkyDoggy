(function (app) {
    'use strict';

    app.controller('recuperarContrasenaCtrl', recuperarContrasenaCtrl);

    recuperarContrasenaCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location'];

    function recuperarContrasenaCtrl($scope, servicioMembresia, servicioNotificaciones, $rootScope, $location) {
        $scope.usuario = {};
        $scope.recuperarContrasena = recuperarContrasena;
        function recuperarContrasena() {
            servicioNotificaciones.mostrarExito("Contraseña recuperada con éxito");
            $location.path('/');
        }

    }

})(angular.module('common.core'));
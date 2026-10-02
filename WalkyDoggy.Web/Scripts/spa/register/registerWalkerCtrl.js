(function (app) {
    'use strict';

    app.controller('registroPaseadorCtrl', registroPaseadorCtrl);

    registroPaseadorCtrl.$inject = ['$scope', 'servicioApi', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location'];

    function registroPaseadorCtrl($scope, servicioApi, servicioMembresia, servicioNotificaciones, $rootScope, $location) {

        $scope.paseador = {};

        $scope.registrar = function registrar() {
            if ($scope.paseador.password == $scope.paseador.confirmPassword) {
                servicioApi.post('/api/walkers/register', $scope.paseador, alRegistrar);
            }
            else {
                servicioNotificaciones.mostrarError('Las contraseñas no coinciden');
            }
        }

        function alRegistrar(resultado) {
            if (resultado.status == 200) {
                servicioMembresia.guardarCredenciales(resultado.data, $scope.paseador.firstName);
            }
            else {
                servicioNotificaciones.mostrarError('No es posible completar la registración. Intente nuevamente.');
            }
        }
    }

})(angular.module('common.core'));

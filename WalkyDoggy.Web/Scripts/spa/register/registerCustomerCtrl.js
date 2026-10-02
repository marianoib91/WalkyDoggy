(function (app) {
    'use strict';

    app.controller('registroClienteCtrl', registroClienteCtrl);

    registroClienteCtrl.$inject = ['$scope', 'servicioApi', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location'];

    function registroClienteCtrl($scope, servicioApi, servicioMembresia, servicioNotificaciones, $rootScope, $location) {

        $scope.cliente = {};






        $scope.registrar = function () {
            if ($scope.cliente.password == $scope.cliente.confirmPassword) {
                servicioApi.post('/api/customers/register', $scope.cliente, alRegistrar)
            }
            else {
                servicioNotificaciones.mostrarError('Las contraseñas no coinciden');
            }
        }

        function alRegistrar(resultado) {
            if (resultado.status == 200) {
                servicioMembresia.guardarCredenciales(resultado.data, $scope.cliente.firstName);
            }
            else {
                servicioNotificaciones.mostrarError('No es posible completar la registración. Intente nuevamente.');
            }
        }

    }

})(angular.module('common.core'));

(function (app) {
    'use strict';

    app.controller('inicioSesionModalCtrl', inicioSesionModalCtrl);

    inicioSesionModalCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location', '$modalInstance'];

    function inicioSesionModalCtrl($scope, servicioMembresia, servicioNotificaciones, $rootScope, $location, $modalInstance) {

        $scope.usuario = {};

        //TODO:Traer el rol en el login para saber a que vista redireccionarlo
        $scope.iniciarSesion = function () {
            servicioMembresia.iniciarSesion($scope.usuario, alIniciarSesion);
        }

        function alIniciarSesion(resultado) {
            if (resultado.data.success) {
                $modalInstance.close();
                resultado.data.password = $scope.usuario.password;
                servicioMembresia.guardarCredenciales(resultado.data, resultado.data.email);               
            }
            else if (resultado.data.blocked) {
                servicioNotificaciones.mostrarError('Tu cuenta está bloqueada. Motivo: ' + (resultado.data.reason || 'no se indicó') + '. Si creés que es un error, contactá a un administrador.');
            }
            else {
                servicioNotificaciones.mostrarError('Imposible iniciar sesión. Intente nuevamente.');
            }
        }
        $scope.cerrarModal = function () {
            $modalInstance.close();
        }
    }

})(angular.module('common.core'));
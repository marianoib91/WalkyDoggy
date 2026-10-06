(function (app) {
    'use strict';

    app.controller('cambiarContrasenaModalCtrl', cambiarContrasenaModalCtrl);

    cambiarContrasenaModalCtrl.$inject = ['$scope', '$modalInstance', 'servicioApi', 'servicioMembresia', 'servicioNotificaciones'];

    function cambiarContrasenaModalCtrl($scope, $modalInstance, servicioApi, servicioMembresia, servicioNotificaciones) {
        $scope.datos = { actual: '', nueva: '', repetida: '' };
        $scope.error = '';
        $scope.guardando = false;

        $scope.cancelar = function () {
            $modalInstance.dismiss('cancel');
        };

        //Devuelve el motivo por el que no se puede enviar, o vacio si esta todo bien
        function validar() {
            var datos = $scope.datos;

            if (!datos.actual) {
                return 'Ingresá tu contraseña actual.';
            }
            if (!datos.nueva || datos.nueva.length < 6 || datos.nueva.length > 50) {
                return 'La contraseña nueva debe tener entre 6 y 50 caracteres.';
            }
            if (datos.nueva === datos.actual) {
                return 'La contraseña nueva tiene que ser distinta de la actual.';
            }
            if (datos.nueva !== datos.repetida) {
                return 'Las contraseñas nuevas no coinciden.';
            }
            return '';
        }

        $scope.guardar = function () {
            $scope.error = validar();
            if ($scope.error) {
                return;
            }

            $scope.guardando = true;
            servicioApi.post('/api/account/changePassword', { currentPassword: $scope.datos.actual, newPassword: $scope.datos.nueva }, function () {
                //Desde ahora la sesion usa la contraseña nueva
                servicioMembresia.actualizarContrasena($scope.datos.nueva);
                servicioNotificaciones.mostrarExito('Cambiaste tu contraseña.');
                $modalInstance.close();
            }, function (error) {
                $scope.guardando = false;
                $scope.error = error.data && error.data[0] ? error.data[0] : 'No se pudo cambiar la contraseña. Intentá de nuevo.';
            });
        };
    }

})(angular.module('common.core'));

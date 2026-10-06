(function (app) {
    'use strict';

    app.controller('raizCtrl', raizCtrl);

    raizCtrl.$inject = ['$scope', '$location', 'servicioMembresia', '$rootScope', '$modal', 'blockUIConfig', 'servicioApi'];
    function raizCtrl($scope, $location, servicioMembresia, $rootScope, $modal, blockUIConfig, servicioApi) {
        blockUIConfig.message = "Cargando ..."

        $scope.userData = {};

        $scope.userData.mostrarDatosUsuario = mostrarDatosUsuario;
        $scope.userData.actualizarDatosUsuario = actualizarDatosUsuario;

        $scope.cerrarSesion = function () {
            servicioMembresia.quitarCredenciales();
            $scope.userData.mostrarDatosUsuario();
            $location.path('/public');
        }

        //El nombre y la foto de quien inició sesión, arriba a la derecha ("Hola, Mariano"). No se guardan en la cookie: se piden al perfil
        //cuando entra otra persona, y de nuevo cuando se guarda el perfil.
        $scope.perfilBarra = { idUsuario: null, nombre: '', foto: null };

        function cargarPerfilBarra(forzar) {
            var usuario = $rootScope.repository.loggedUser;
            if (!forzar && $scope.perfilBarra.idUsuario === usuario.id) {
                return;
            }

            $scope.perfilBarra = { idUsuario: usuario.id, nombre: usuario.roleId == '1' ? 'Administrador' : '', foto: null };
            if (usuario.roleId == '1') {
                return;
            }

            var ruta = usuario.roleId == '2' ? '/api/customers/getByUserId' : '/api/walkers/getByUserId';
            servicioApi.get(ruta, { params: { userId: usuario.id } }, function (resultado) {
                //Si mientras tanto cambio la sesion, la respuesta ya no corresponde
                if ($scope.perfilBarra.idUsuario === usuario.id) {
                    $scope.perfilBarra.nombre = resultado.data.firstName || usuario.email;
                    $scope.perfilBarra.foto = resultado.data.profileImage || null;
                }
            });
        }

        $rootScope.$on('perfil:actualizado', function () {
            if ($scope.userData.haySesion) {
                cargarPerfilBarra(true);
            }
        });

        function mostrarDatosUsuario() {
            $scope.userData.haySesion = servicioMembresia.haySesion();

            if ($scope.userData.haySesion) {
                $scope.roleId = $rootScope.repository.loggedUser.roleId;
                $scope.email = $rootScope.repository.loggedUser.email;
                $scope.name = $rootScope.repository.loggedUser.name;
                cargarPerfilBarra(false);
            }
            else {
                $scope.perfilBarra = { idUsuario: null, nombre: '', foto: null };
            }
        }

        function actualizarDatosUsuario(nombreUsuario, emailUsuario) {
            $scope.userData.haySesion = servicioMembresia.haySesion();
            if ($scope.userData.haySesion) {
                $rootScope.repository.loggedUser.email = emailUsuario;
                $rootScope.repository.loggedUser.name = nombreUsuario;
                $scope.email = emailUsuario;
                $scope.name = nombreUsuario;
            }
        }

        $scope.abrirModalInicioSesion = function () {
            $modal.open({
                templateUrl: 'scripts/spa/account/login-modal.html',
                controller: 'inicioSesionModalCtrl',
                scope: $scope
            }).result.then(function ($scope) {
            }, function () {
            });
        }

        $scope.abrirModalCambioContrasena = function () {
            $modal.open({
                templateUrl: 'scripts/spa/account/changePasswordModal.html',
                controller: 'cambiarContrasenaModalCtrl',
                size: 'sm'
            }).result.then(function () { }, function () { });
        }

        $scope.abrirModalRegistro =function () {
            $modal.open({
                templateUrl: 'scripts/spa/register/register-modal.html',
                controller: 'registroModalCtrl',
                scope: $scope
            }).result.then(function ($scope) {
            }, function () {
            });
        }

        $scope.userData.mostrarDatosUsuario();

        //La barra superior (botones de sesion y menu) se actualiza al llegar a cualquier pantalla,
        //asi no depende de que la pantalla de destino del login se acuerde de hacerlo
        $scope.$on('$routeChangeSuccess', mostrarDatosUsuario);
    }

})(angular.module('walkyDoggy'));
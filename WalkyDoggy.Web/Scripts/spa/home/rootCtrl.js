(function (app) {
    'use strict';

    app.controller('raizCtrl', raizCtrl);

    raizCtrl.$inject = ['$scope', '$location', 'servicioMembresia', '$rootScope', '$modal', 'blockUIConfig'];
    function raizCtrl($scope, $location, servicioMembresia, $rootScope, $modal, blockUIConfig) {
        blockUIConfig.message = "Cargando ..."

        $scope.userData = {};

        $scope.userData.mostrarDatosUsuario = mostrarDatosUsuario;
        $scope.userData.actualizarDatosUsuario = actualizarDatosUsuario;

        $scope.cerrarSesion = function () {
            servicioMembresia.quitarCredenciales();
            $scope.userData.mostrarDatosUsuario();
            $location.path('/public');
        }

        function mostrarDatosUsuario() {
            $scope.userData.haySesion = servicioMembresia.haySesion();

            if ($scope.userData.haySesion) {
                $scope.roleId = $rootScope.repository.loggedUser.roleId;
                $scope.email = $rootScope.repository.loggedUser.email;
                $scope.name = $rootScope.repository.loggedUser.name;
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

        $scope.abrirModalRegistro = function () {
            $modal.open({
                templateUrl: 'scripts/spa/register/register-modal.html',
                controller: 'registroModalCtrl',
                scope: $scope
            }).result.then(function ($scope) {
            }, function () {
            });
        }

        $scope.userData.mostrarDatosUsuario();
    }

})(angular.module('walkyDoggy'));
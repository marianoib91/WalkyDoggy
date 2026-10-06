(function (app) {
    'use strict';

    app.controller('bienvenidaClienteCtrl', bienvenidaClienteCtrl);

    bienvenidaClienteCtrl.$inject = ['$scope', '$rootScope', 'servicioApi'];

    //Pantalla que se muestra al terminar el registro del cliente: le da la bienvenida y lo invita a cargar a sus mascotas
    function bienvenidaClienteCtrl($scope, $rootScope, servicioApi) {
        $scope.cliente = null;

        var idUsuario = $rootScope.repository.loggedUser.id;
        servicioApi.get('/api/customers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
            $scope.cliente = resultado.data;
        });
    }

})(angular.module('walkyDoggy'));

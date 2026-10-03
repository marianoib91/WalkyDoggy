(function (app) {
    'use strict';

    app.controller('bienvenidaPaseadorCtrl', bienvenidaPaseadorCtrl);

    bienvenidaPaseadorCtrl.$inject = ['$scope', '$rootScope', 'servicioApi'];

    //Pantalla que se muestra al terminar el registro del paseador: lo felicita y lo invita a definir sus horarios
    function bienvenidaPaseadorCtrl($scope, $rootScope, servicioApi) {
        $scope.paseador = null;

        var idUsuario = $rootScope.repository.loggedUser.id;
        servicioApi.get('/api/walkers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
            $scope.paseador = resultado.data;
        });
    }

})(angular.module('walkyDoggy'));

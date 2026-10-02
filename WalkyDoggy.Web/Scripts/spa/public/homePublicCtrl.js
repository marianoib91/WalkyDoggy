(function (app) {
    'use strict';

    app.controller('inicioPublicoCtrl', inicioPublicoCtrl);

    inicioPublicoCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones'];

    function inicioPublicoCtrl($scope, servicioApi, servicioNotificaciones) {

        //init();

        //function init() {
        //    $scope.geocode = {
        //        streetName: "Leandro N. Alem",
        //        streetNumber: "1220",
        //        cityName: "Rosario",
        //        provinceName: "Santa Fe"
        //    }
        //    apiService.post('/api/geocode/getlocation', $scope.geocode, onLoadLocationCompleted)
        //}

        //function onLoadLocationCompleted(results) {
        //    console.log(results.data);
        //}

    }

})(angular.module('walkyDoggy'));
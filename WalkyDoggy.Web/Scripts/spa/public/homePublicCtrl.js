(function (app) {
    'use strict';

    app.controller('homePublicCtrl', homePublicCtrl);

    homePublicCtrl.$inject = ['$scope', 'apiService', 'notificationService'];

    function homePublicCtrl($scope, apiService, notificationService) {

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
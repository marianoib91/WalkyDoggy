(function (app) {
    'use strict';

    app.controller('indexCtrl', indexCtrl);

    indexCtrl.$inject = ['$scope', 'apiService', 'notificationService', '$rootScope'];

    function indexCtrl($scope, apiService, notificationService, $rootScope) {

        $scope.userData.displayUserInfo();
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.walkerId = $rootScope.repository.loggedUser.walkerId;
        $scope.customerId = $rootScope.repository.loggedUser.customerId;
        var userId = $rootScope.repository.loggedUser.id;
        $scope.walkers = {};
        $scope.hasDistances = false;
        $scope.prices = {};
        $scope.provinces = {};
        $scope.cities = {};
        $scope.walks = {};
        $scope.disableCities = true;

        init();

        function init() {
            apiService.get('/api/prices/getAll', null, onLoadPricesCompleted);
            apiService.get('/api/provinces/getAll', null, onLoadProvincesCompleted);
            //Cliente
            if ($scope.roleId == '2') {
                //Los paseadores llegan ordenados por cercania al domicilio del cliente
                apiService.get('/api/walkers/getAllOrderedByDistance', { params: { customerId: $scope.customerId } }, onLoadWalkersCompleted);
            }

            //Paseador
            if ($scope.roleId == '3') {
                var config = {
                    params: {
                        walkerId: $scope.walkerId
                    }
                }
                apiService.get('/api/walks/getAllForCurrentDay', config, onLoadWalksCompleted);
            }
        }

        function onLoadPricesCompleted(result) {
            $scope.prices = result.data;
        }

        function onLoadProvincesCompleted(result) {
            $scope.provinces = result.data;
        }

        $scope.loadCities = function (provinceId) {
            var config = {
                params: {
                    provinceId: provinceId
                }
            }

            apiService.get('/api/cities/getAllByProvinceId/', config, onLoadCitiesCompleted);
        }

        function onLoadCitiesCompleted(result) {
            $scope.cities = result.data;
            $scope.disableCities = false;
        }

        function onLoadWalksCompleted(response) {
            $scope.walks = response.data;
        }

        function onLoadWalkersCompleted(result) {
            $scope.walkers = result.data;
            $scope.hasDistances = result.data.some(function (walker) {
                return walker.distanceKm !== null && walker.distanceKm !== undefined;
            });
        }
    }

})(angular.module('walkyDoggy'));
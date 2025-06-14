(function (app) {
    'use strict';

    app.controller('profileCtrl', profileCtrl);

    profileCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', '$rootScope', '$location'];

    function profileCtrl($scope, membershipService, notificationService, apiService, $rootScope, $location) {
        var userRoleId = $rootScope.repository.loggedUser.roleId;
        $scope.user = null;
        $scope.cities = {};
        $scope.provinces = {};
       

        init();

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
        }

        function onLoadProvincesCompleted(result) {
            $scope.provinces = result.data;
        }

        function init() {
            var userEmail = $rootScope.repository.loggedUser.email;
            var userId = $rootScope.repository.loggedUser.id;

            var config = {
                params: {
                    userId: userId
                }
            }

            //2=Customer
            if (userRoleId == 2) {
                apiService.get('/api/customers/getByUserId', config, onLoadUserCompleted);
            }
                //3=Walker
            else if (userRoleId == 3) {
                apiService.get('/api/walkers/getByUserId', config, onLoadUserCompleted);
               
            }

            apiService.get('/api/provinces/getAll', null, onLoadProvincesCompleted);


        }

    

        function onLoadUserCompleted(results) {
            $scope.user = results.data;
            $scope.user.phone = parseFloat($scope.user.phone, 10);
            $scope.user.streetNumber = parseFloat($scope.user.streetNumber, 10);
            var config = {
                params: {
                    provinceId: $scope.user.provinceId
                }
            }
            apiService.get('/api/cities/getAllByProvinceId/', config, onLoadCitiesCompleted);
        }


        $scope.updateProfile = function () {
            //Customer
            if (userRoleId == 2) {
                apiService.post('/api/customers/update', $scope.user, onUpdateUserCompleted);
            }
                //Walker
            else if (userRoleId == 3) {
                apiService.post('/api/walkers/update', $scope.user, onUpdateUserCompleted);
            }

        }

        function onUpdateUserCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('Perfil actualizado con éxito');
                // $location.path('/');
                console.log();
            }
            else {
                notificationService.displayError('No se pudo actualizar el perfil. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
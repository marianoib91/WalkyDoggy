(function (app) {
    'use strict';

    app.controller('petsEditCtrl', petsEditCtrl);

    petsEditCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', 'fileUploadService', '$rootScope', '$location', '$routeParams'];

    function petsEditCtrl($scope, membershipService, notificationService, apiService, fileUploadService, $rootScope, $location, $routeParams) {

        $scope.petId = $routeParams.id;
        $scope.userId = $rootScope.repository.loggedUser.id;
        $scope.customerId = $rootScope.repository.loggedUser.customerId;

        $scope.pet = {};
        $scope.breeds = {};
        $scope.sizes = {};
        var pendingPhoto = null;
        init();

        $scope.previewImage = null;

        $scope.onPhotoSelected = function ($files) {
            if (!$files || !$files.length) {
                return;
            }

            if ($scope.previewImage) {
                URL.revokeObjectURL($scope.previewImage);
            }
            $scope.previewImage = URL.createObjectURL($files[0]);

            if ($scope.pet.id) {
                fileUploadService.uploadProfileImage($files, 'pet', $scope.pet.id, function (profileImage) {
                    $scope.pet.profileImage = profileImage;
                });
            } else {
                pendingPhoto = $files;
            }
        }

        function init() {
        var config = {
                params: {
                    id: $scope.petId
                }
            }
            apiService.get('/api/pets/getById', config, onLoadPetCompleted);
            //Traer las razas y los tamaños de la BD
            apiService.get('/api/breeds/getAll', null, onLoadBreedsCompleted);
            apiService.get('/api/sizes/getAll', null, onLoadSizesCompleted);

        }

        function onLoadCustomerCompleted(result) {
            $scope.customerId = result.data.id;
            
        }

        function onLoadPetCompleted(result) {
            $scope.pet = result.data;
        }

        function onLoadBreedsCompleted(result) {
            $scope.breeds = result.data;
        }

        function onLoadSizesCompleted(result) {
            $scope.sizes = result.data;
        }

        $scope.updatePet = function () {
            if ($scope.pet.id) {
                apiService.post('/api/pets/update', $scope.pet, onUpdatePetCompleted);
            }
            else {
                $scope.pet.customerId = $scope.customerId;
                apiService.post('/api/pets/register', $scope.pet, onRegisterPetCompleted);
            }
        }
        function onRegisterPetCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess(response.data.name + ' se ha registrado exitosamente.');
                if (pendingPhoto) {
                    fileUploadService.uploadProfileImage(pendingPhoto, 'pet', response.data.id, function () {
                        $location.path('/pets/list');
                    });
                } else {
                    $location.path('/pets/list');
                }
            }
            else {
                notificationService.displayError('No se pudo registrar a tu mascota. Intente nuevamente');
            }
        }
        function onUpdatePetCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('Cambios guardados con éxito');
                $location.path('/pets/list');
            }
            else {
                notificationService.displayError('No se pudieron guardar los cambios realizados. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
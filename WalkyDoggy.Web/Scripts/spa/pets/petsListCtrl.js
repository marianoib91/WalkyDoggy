(function (app) {
    'use strict';

    app.controller('petsListCtrl', petsListCtrl);

    petsListCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', '$rootScope', '$location', 'sweetAlert'];

    function petsListCtrl($scope, membershipService, notificationService, apiService, $rootScope, $location, sweetAlert) {

        //Se obtiene el id del cliente guardado en el indexCtrl
        $scope.userId = $rootScope.repository.loggedUser.id;
        $scope.customerId = $rootScope.repository.loggedUser.customerId;
        $scope.pets = {};

        init();

        function init() {
            var config = {
                params: {
                    customerId: $scope.customerId
                }
            }
            apiService.get('/api/pets/getAllByCustomerId/', config, onLoadPetsCompleted);
        }

        function onLoadPetsCompleted(result) {
            $scope.pets = result.data;
        }

        $scope.edit = function (id) {
            $location.path('/pets/edit/' + id);
        }

        $scope.delete = function (id) {
            sweetAlert.swal({
                title: "Eliminar mascota",
                text: "¿Desea eliminar la mascota seleccionada?",
                type: "error",
                showCancelButton: true,
                confirmButtonText: "Aceptar",
                cancelButtonText: "Cancelar"
            }).then(function (isConfirm) {
                if (isConfirm) {
                    var r = true;
                }
                if (r == true) {
                    apiService.remove('/api/pets/' + id, onDeletePetCompleted);
                }
            });

        }
        function onDeletePetCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('Tu mascota se ha eliminado con éxito');
                var config = {
                    params: {
                        customerId: $scope.customerId
                    }
                }
                apiService.get('/api/pets/getAllByCustomerId/', config, onLoadPetsCompleted);
            }
            else {
                notificationService.displayError('No se pudo eliminar tu mascota. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
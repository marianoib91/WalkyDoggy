(function (app) {
    'use strict';

    app.controller('step1Ctrl', step1Ctrl);

    step1Ctrl.$inject = ['$scope', 'apiService', 'notificationService', '$rootScope', '$location'];

    function step1Ctrl($scope, apiService, notificationService, $rootScope, $location) {

        $scope.pets = {};
        $scope.walk = {};
        init();

        $scope.opts = {
            singleDatePicker: true,
            showDropdowns: true,
            minDate: new Date()
        };

        function init() {
            //Se trae al cliente o al paseador para guardar el id en rootScope
            $scope.walk.date = new Date();
            var config = {
                params: {
                    userId: $rootScope.repository.loggedUser.id
                }
            }
            apiService.get('/api/customers/getByUserId/', config, onLoadCustomerCompleted);
        }
        function onLoadCustomerCompleted(result) {
            $scope.customer = result.data;
            var config = {
                params: {
                    customerId: $scope.customer.id
                }
            }
            apiService.get('/api/pets/getAllByCustomerId/', config, onLoadPetsCompleted);
        }

        function onLoadPetsCompleted(result) {
            $scope.pets = result.data;
        }

        $scope.validateTimeFrom = function () {
        //TODO: Validar que si selecciona un horario del dia actual no sea menor a la hora actual
            var date = new Date;
            var currentHour = date.getHours();
            var selectedHour =Number($scope.walk.timeFrom.split(':')[0]);
            //if (currentHour >= selectedHour) {
            //    $scope.walk.timeFrom = null;
            //    notificationService.displayError("Debe seleccionar un horario posterior a la hora actual.");
            //}
        }

        $scope.submit = function () {
            var selectedPets = [];
            for (var i = 0; i < $scope.pets.length; i++) {
                if ($scope.pets[i].selectedPet == true) {
                    selectedPets.push($scope.pets[i]);
                }
            }
            if (selectedPets.length > 0) {
                var availableWalkersCriteria = {
                    date: $scope.walk.date,
                    timeFrom: $scope.walk.timeFrom,
                    selectedPets: selectedPets
                }

                apiService.post('/api/walks/validatePetsInWalks', availableWalkersCriteria, onValidatePetsCompleted);

            }
            else {
                notificationService.displayError("Debe seleccionar una mascota para su paseo.");
            }
        }

        function onValidatePetsCompleted(response) {
            if (response.data.id == 0) {
                //Se guarda en localStorage la fecha y hora del paseo para poder traer los paseadores en el siguiente paso
                localStorage.setItem('walkDate', JSON.stringify($scope.walk.date));
                localStorage.setItem('walkTimeFrom', JSON.stringify($scope.walk.timeFrom));
                $location.path('/walks/step-2');
            }
            else {
                notificationService.displayError(response.data.petName + ' ya tiene reservado un paseo a la misma hora y día seleccionados.')
            }
        }
    }

})(angular.module('walkyDoggy'));
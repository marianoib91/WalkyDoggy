(function (app) {
    'use strict';

    app.controller('step1Ctrl', step1Ctrl);

    step1Ctrl.$inject = ['$scope', 'apiService', 'notificationService', '$rootScope', '$location', '$routeParams'];

    function step1Ctrl($scope, apiService, notificationService, $rootScope, $location, $routeParams) {

        //Indice de moment().day(): 0 = domingo
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var walkerId = $routeParams.walkerId;
        var draft = $rootScope.walkDraft;
        var timesRequest = 0;

        $scope.pets = {};
        $scope.walk = {};
        $scope.walker = null;
        $scope.times = [];
        $scope.workDayNames = [];
        $scope.workDaysText = '';
        $scope.hasSchedule = true;
        $scope.loadingTimes = false;

        //Donde se retira a las mascotas: en el domicilio del cliente ('home') o en otra direccion ('other')
        $scope.pickup = { mode: 'home', other: {} };
        $scope.homeAddress = '';

        //El calendario solo habilita los dias en los que el paseador tiene una jornada laboral
        $scope.opts = {
            singleDatePicker: true,
            showDropdowns: true,
            minDate: new Date(),
            isInvalidDate: function (date) {
                return $scope.workDayNames.indexOf(dayNames[date.day()]) === -1;
            }
        };

        if (!walkerId) {
            notificationService.displayError('Elegí un paseador para reservar un paseo.');
            $location.search({}).path('/');
            return;
        }

        //Si se vuelve del paso 2 se conserva lo que ya se habia elegido
        if (draft && draft.walkerId == walkerId) {
            $scope.walk.date = moment(draft.date, 'YYYY-MM-DD');
            $scope.walk.timeFrom = draft.timeFrom;

            if (draft.pickup && !draft.pickup.isHome) {
                $scope.pickup.mode = 'other';
                $scope.pickup.other = angular.copy(draft.pickup);
            }
        }

        init();

        function init() {
            apiService.get('/api/walkers/getDetail', { params: { id: walkerId } }, onLoadWalkerCompleted, onLoadWalkerFailed);
            apiService.get('/api/workDays/getAllByWalkerId', { params: { walkerId: walkerId } }, onLoadWorkDaysCompleted);
            apiService.get('/api/customers/getByUserId/', { params: { userId: $rootScope.repository.loggedUser.id } }, onLoadCustomerCompleted);
        }

        function onLoadWalkerCompleted(result) {
            $scope.walker = result.data;
        }

        function onLoadWalkerFailed() {
            notificationService.displayError('No se encontró al paseador seleccionado.');
            $location.search({}).path('/');
        }

        function onLoadWorkDaysCompleted(result) {
            var names = [];
            angular.forEach(result.data, function (workDay) {
                if (names.indexOf(workDay.dayOfWeek) === -1) {
                    names.push(workDay.dayOfWeek);
                }
            });

            $scope.workDayNames = names;
            $scope.hasSchedule = names.length > 0;
            $scope.workDaysText = dayNames.filter(function (name) {
                return names.indexOf(name) !== -1;
            }).join(', ');

            //Se propone la primera fecha en la que trabaja, a menos que ya haya una elegida
            if (!$scope.walk.date && $scope.hasSchedule) {
                for (var i = 0; i < 60; i++) {
                    var candidate = moment().add(i, 'days');
                    if (names.indexOf(dayNames[candidate.day()]) !== -1) {
                        $scope.walk.date = candidate;
                        break;
                    }
                }
            }
        }

        function onLoadCustomerCompleted(result) {
            $scope.customer = result.data;
            $scope.homeAddress = formatAddress($scope.customer);
            apiService.get('/api/pets/getAllByCustomerId/', { params: { customerId: $scope.customer.id } }, onLoadPetsCompleted);
        }

        function onLoadPetsCompleted(result) {
            $scope.pets = result.data;

            if (draft && draft.walkerId == walkerId) {
                var selectedIds = draft.pets.map(function (pet) { return pet.id; });
                angular.forEach($scope.pets, function (pet) {
                    pet.selectedPet = selectedIds.indexOf(pet.id) !== -1;
                });
            }
        }

        //Cada vez que cambia la fecha se traen los horarios libres de ese paseador para ese dia
        $scope.$watch('walk.date', function (date) {
            if (!date) {
                $scope.times = [];
                return;
            }

            var request = ++timesRequest;
            $scope.loadingTimes = true;
            var config = {
                params: {
                    walkerId: walkerId,
                    date: moment(date).format('YYYY-MM-DD')
                }
            };

            apiService.get('/api/walkers/getAvailableTimes', config, function (result) {
                if (request !== timesRequest) {
                    return;
                }
                $scope.times = result.data;
                $scope.loadingTimes = false;
                if ($scope.times.indexOf($scope.walk.timeFrom) === -1) {
                    $scope.walk.timeFrom = null;
                }
            }, function () {
                if (request === timesRequest) {
                    $scope.loadingTimes = false;
                }
            });
        });

        function formatAddress(address) {
            var line = [address.streetName, address.streetNumber].filter(Boolean).join(' ');
            return [line, address.cityName, address.provinceName].filter(Boolean).join(', ');
        }

        //Devuelve la direccion de retiro elegida, o null si eligio "otra direccion" y no la completo
        function buildPickup() {
            var source = $scope.customer;
            var isHome = $scope.pickup.mode !== 'other';

            if (!isHome) {
                source = $scope.pickup.other;
                if (!source.cityId || !source.streetName || !source.streetNumber) {
                    return null;
                }
            }

            return {
                isHome: isHome,
                streetName: source.streetName,
                streetNumber: source.streetNumber,
                cityId: source.cityId,
                cityName: source.cityName,
                provinceName: source.provinceName,
                latitude: source.latitude,
                longitude: source.longitude
            };
        }

        $scope.submit = function () {
            var selectedPets = [];
            for (var i = 0; i < $scope.pets.length; i++) {
                if ($scope.pets[i].selectedPet == true) {
                    selectedPets.push($scope.pets[i]);
                }
            }

            if (selectedPets.length === 0) {
                notificationService.displayError('Debe seleccionar una mascota para su paseo.');
                return;
            }

            var pickup = buildPickup();
            if (!pickup) {
                notificationService.displayError('Elegí la dirección de retiro de la lista de sugerencias.');
                return;
            }

            var date = moment($scope.walk.date).format('YYYY-MM-DD');
            var criteria = {
                date: date,
                timeFrom: $scope.walk.timeFrom,
                selectedPets: selectedPets
            };

            apiService.post('/api/walks/validatePetsInWalks', criteria, function (response) {
                if (response.data.id == 0) {
                    //Los datos elegidos viajan al paso 2 para confirmar la reserva
                    $rootScope.walkDraft = {
                        walkerId: walkerId,
                        date: date,
                        timeFrom: $scope.walk.timeFrom,
                        pickup: pickup,
                        pets: selectedPets.map(function (pet) {
                            return { id: pet.id, name: pet.name, profileImage: pet.profileImage };
                        })
                    };
                    $location.search({}).path('/walks/step-2');
                }
                else {
                    notificationService.displayError(response.data.petName + ' ya tiene reservado un paseo a la misma hora y día seleccionados.');
                }
            });
        };
    }

})(angular.module('walkyDoggy'));

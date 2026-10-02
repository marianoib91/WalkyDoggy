(function (app) {
    'use strict';

    app.controller('walkerProfileCtrl', walkerProfileCtrl);

    walkerProfileCtrl.$inject = ['$scope', 'apiService', 'notificationService', '$routeParams', '$rootScope'];

    function walkerProfileCtrl($scope, apiService, notificationService, $routeParams, $rootScope) {
        var pageSize = 8;
        var days = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

        $scope.walkerId = Number($routeParams.id);
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.isCustomer = $scope.roleId == 2;
        $scope.isOwnProfile = $scope.roleId == 3 && $rootScope.repository.loggedUser.walkerId == $scope.walkerId;

        $scope.walker = null;
        $scope.notFound = false;
        $scope.availability = [];
        $scope.summary = null;
        $scope.ratings = [];
        $scope.total = 0;
        $scope.hasMore = false;
        $scope.loadingRatings = false;
        $scope.filter = { sort: 'recent', stars: '' };

        var page = 1;

        init();

        function init() {
            apiService.get('/api/walkers/getDetail', { params: { id: $scope.walkerId } }, function (result) {
                if (!result.data) {
                    $scope.notFound = true;
                    return;
                }
                $scope.walker = result.data;
            });

            apiService.get('/api/workDays/getAllByWalkerId', { params: { walkerId: $scope.walkerId } }, function (result) {
                $scope.availability = buildAvailability(result.data);
            });

            apiService.get('/api/ratings/summary', { params: { walkerId: $scope.walkerId } }, function (result) {
                $scope.summary = result.data;
            });

            loadRatings(true);
        }

        //Un renglon por dia con sus franjas: "08:00 a 14:00 · 16:00 a 20:00" o "Todo el día"
        function buildAvailability(workDays) {
            return days.map(function (day) {
                var ranges = workDays.filter(function (workDay) { return workDay.dayOfWeek === day; })
                                     .sort(function (a, b) { return a.timeFrom < b.timeFrom ? -1 : 1; });

                var text = ranges.map(function (range) {
                    return range.timeFrom === '00:00' && range.timeUntil === '24:00'
                        ? 'Todo el día'
                        : range.timeFrom + ' a ' + range.timeUntil;
                }).join(' · ');

                return { day: day, works: ranges.length > 0, text: text };
            });
        }

        function loadRatings(reset) {
            if (reset) {
                page = 1;
            }

            $scope.loadingRatings = true;
            var params = { walkerId: $scope.walkerId, sort: $scope.filter.sort, page: page, pageSize: pageSize };
            if ($scope.filter.stars) {
                params.stars = $scope.filter.stars;
            }

            apiService.get('/api/ratings/list', { params: params }, function (result) {
                $scope.ratings = reset ? result.data.items : $scope.ratings.concat(result.data.items);
                $scope.total = result.data.total;
                $scope.hasMore = result.data.hasMore;
                $scope.loadingRatings = false;
            }, function () {
                $scope.loadingRatings = false;
                notificationService.displayError('No se pudieron cargar las opiniones.');
            });
        }

        $scope.refilter = function () {
            loadRatings(true);
        };

        //Lleva a la seccion de opiniones (el enlace "N calificaciones" de la cabecera)
        $scope.goToOpinions = function () {
            var section = document.getElementById('opiniones');
            if (section && section.scrollIntoView) {
                section.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        };

        $scope.loadMore = function () {
            page++;
            loadRatings(false);
        };

        $scope.formatAverage = function (value) {
            return value === null || value === undefined ? '' : Number(value).toFixed(1).replace('.', ',');
        };

        //Ancho de la barra de cada cantidad de estrellas, en proporcion al total de valoraciones
        $scope.barWidth = function (bucket) {
            return $scope.summary && $scope.summary.count > 0 ? Math.round(bucket.count * 100 / $scope.summary.count) : 0;
        };

        $scope.dateText = function (date) {
            return moment(date).format('DD/MM/YYYY');
        };

        $scope.countText = function (count, singular, plural) {
            return count + ' ' + (count === 1 ? singular : plural);
        };
    }

})(angular.module('walkyDoggy'));

(function (app) {
    'use strict';

    app.controller('workConditionsCtrl', workConditionsCtrl);

    workConditionsCtrl.$inject = ['$scope', 'notificationService', 'apiService', '$rootScope', '$window'];

    function workConditionsCtrl($scope, notificationService, apiService, $rootScope, $window) {
        var walkerId = $rootScope.repository.loggedUser.walkerId;
        var painting = null;

        $scope.days = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
        $scope.hours = [];
        for (var hour = 0; hour < 24; hour++) {
            $scope.hours.push(hour);
        }

        //week[dia][hora] = true si trabaja en esa hora (la hora N va de N:00 a N+1:00)
        $scope.week = {};
        angular.forEach($scope.days, function (day) { $scope.week[day] = emptyDay(); });

        $scope.walker = null;
        $scope.form = { amount: null, description: '' };
        $scope.saving = false;
        $scope.loaded = false;

        init();

        function emptyDay() {
            var cells = [];
            for (var hour = 0; hour < 24; hour++) { cells.push(false); }
            return cells;
        }

        function pad(hour) {
            return (hour < 10 ? '0' : '') + hour + ':00';
        }

        function init() {
            apiService.get('/api/workDays/getAllByWalkerId', { params: { walkerId: walkerId } }, function (result) {
                angular.forEach(result.data, function (workDay) {
                    var from = Number(workDay.timeFrom.split(':')[0]);
                    var until = Number(workDay.timeUntil.split(':')[0]);
                    if ($scope.week[workDay.dayOfWeek]) {
                        for (var hour = from; hour < until && hour < 24; hour++) {
                            $scope.week[workDay.dayOfWeek][hour] = true;
                        }
                    }
                });
            });

            apiService.get('/api/walkers/getByUserId', { params: { userId: $rootScope.repository.loggedUser.id } }, function (result) {
                $scope.walker = result.data;
                $scope.form.amount = result.data.amount;
                $scope.form.description = result.data.description;
                $scope.loaded = true;
            });
        }

        /* ---------- Grilla semanal: se pinta arrastrando con el mouse o tocando cada hora ---------- */

        $scope.startPaint = function (day, hour, $event) {
            if ($event) {
                $event.preventDefault();
            }
            painting = { value: !$scope.week[day][hour] };
            $scope.week[day][hour] = painting.value;
        };

        $scope.paintOver = function (day, hour) {
            if (painting) {
                $scope.week[day][hour] = painting.value;
            }
        };

        function stopPainting() {
            painting = null;
        }
        $window.addEventListener('mouseup', stopPainting);
        $scope.$on('$destroy', function () { $window.removeEventListener('mouseup', stopPainting); });

        $scope.fillDay = function (day, value) {
            for (var hour = 0; hour < 24; hour++) { $scope.week[day][hour] = value; }
        };

        $scope.fillAll = function (value) {
            angular.forEach($scope.days, function (day) { $scope.fillDay(day, value); });
        };

        $scope.copyToAll = function (source) {
            angular.forEach($scope.days, function (day) {
                for (var hour = 0; hour < 24; hour++) { $scope.week[day][hour] = $scope.week[source][hour]; }
            });
            notificationService.displaySuccess('Copiaste el horario del ' + source.toLowerCase() + ' a todos los días.');
        };

        //Franjas continuas de un dia: [{ from: 8, until: 14 }, ...] (until = 24 es la medianoche)
        function rangesOf(day) {
            var ranges = [];
            var hour = 0;
            while (hour < 24) {
                if (!$scope.week[day][hour]) { hour++; continue; }
                var start = hour;
                while (hour < 24 && $scope.week[day][hour]) { hour++; }
                ranges.push({ from: start, until: hour });
            }
            return ranges;
        }

        $scope.summaryOf = function (day) {
            var ranges = rangesOf(day);
            if (ranges.length === 0) {
                return 'No trabajás';
            }
            if (ranges.length === 1 && ranges[0].from === 0 && ranges[0].until === 24) {
                return 'Todo el día';
            }
            return ranges.map(function (range) { return pad(range.from) + ' a ' + pad(range.until); }).join(' · ');
        };

        $scope.weeklyHours = function () {
            var total = 0;
            angular.forEach($scope.days, function (day) {
                for (var hour = 0; hour < 24; hour++) { if ($scope.week[day][hour]) { total++; } }
            });
            return total;
        };

        $scope.workedDays = function () {
            return $scope.days.filter(function (day) { return rangesOf(day).length > 0; }).length;
        };

        /* ---------- Guardar ---------- */

        $scope.validRate = function () {
            var amount = Number($scope.form.amount);
            return $scope.form.amount !== null && $scope.form.amount !== '' && amount >= 1 && amount <= 1000000;
        };

        $scope.submit = function () {
            if ($scope.saving) {
                return;
            }
            if (!$scope.validRate()) {
                notificationService.displayError('Ingresá una tarifa por hora válida (entre $1 y $1.000.000).');
                return;
            }
            if (!$scope.form.description || !$scope.form.description.trim()) {
                notificationService.displayError('Contales a tus clientes sobre vos: la descripción es obligatoria.');
                return;
            }

            var ranges = [];
            angular.forEach($scope.days, function (day) {
                angular.forEach(rangesOf(day), function (range) {
                    ranges.push({ dayOfWeek: day, timeFrom: pad(range.from), timeUntil: pad(range.until) });
                });
            });

            $scope.saving = true;
            apiService.post('/api/workDays/saveWeek', { walkerId: walkerId, ranges: ranges }, function () {
                $scope.walker.amount = Number($scope.form.amount);
                $scope.walker.description = $scope.form.description.trim();

                apiService.post('/api/walkers/update', $scope.walker, function () {
                    $scope.saving = false;
                    notificationService.displaySuccess('Condiciones laborales guardadas.');
                }, onSaveFailed);
            }, onSaveFailed);
        };

        function onSaveFailed(error) {
            $scope.saving = false;
            var message = error && error.data && error.data[0] ? error.data[0] : 'No se pudieron guardar las condiciones laborales. Intentá nuevamente.';
            notificationService.displayError(message);
        }
    }

})(angular.module('common.core'));

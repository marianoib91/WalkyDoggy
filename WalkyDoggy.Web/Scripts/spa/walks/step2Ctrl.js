(function (app) {
    'use strict';

    app.controller('step2Ctrl', step2Ctrl);

    step2Ctrl.$inject = ['$scope', 'apiService', 'notificationService', '$rootScope'];

    function step2Ctrl($scope, apiService, notificationService, $rootScope) {
        $scope.walkers = {};

        init();

        function init() {
            //Se trae la fecha y el horario del paso anterior
            var walkDate = JSON.parse(localStorage.getItem('walkDate'));
            var walkTimeFrom = JSON.parse(localStorage.getItem('walkTimeFrom'));

            var availableWalkersCriteria= {
                date: walkDate,
                timeFrom:walkTimeFrom
            }

            apiService.post('/api/walkers/getAvailableWalkers', availableWalkersCriteria, onLoadWalkersCompleted);
        }
      

        function onLoadWalkersCompleted(response) {
            $scope.walkers = response.data;
        }
    }

})(angular.module('walkyDoggy'));
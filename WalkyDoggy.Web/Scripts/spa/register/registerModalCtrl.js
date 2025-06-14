(function (app) {
    'use strict';

    app.controller('registerModalCtrl', registerModalCtrl);

    registerModalCtrl.$inject = ['$scope', 'membershipService', 'notificationService', '$rootScope', '$location', '$modalInstance'];

    function registerModalCtrl($scope, membershipService, notificationService, $rootScope, $location, $modalInstance) {

        $scope.register = function (redirectCode) {
            if (redirectCode == 1) {
                $modalInstance.close();
                $location.path('/register/walker');
            }
            else if (redirectCode == 2) {
                $modalInstance.close();
                $location.path('/register/customer');
            }
        }

        $scope.closeModal = function () {
            $modalInstance.close();
        }
    }

})(angular.module('common.core'));
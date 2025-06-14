(function (app) {
    'use strict';

    app.controller('rootCtrl', rootCtrl);

    rootCtrl.$inject = ['$scope', '$location', 'membershipService', '$rootScope', '$modal', 'blockUIConfig'];
    function rootCtrl($scope, $location, membershipService, $rootScope, $modal, blockUIConfig) {
        blockUIConfig.message = "Cargando ..."

        $scope.userData = {};

        $scope.userData.displayUserInfo = displayUserInfo;
        $scope.userData.updateUserInfo = updateUserInfo;

        $scope.logout = function () {
            membershipService.removeCredentials();
            $scope.userData.displayUserInfo();
            $location.path('/public');
        }

        function displayUserInfo() {
            $scope.userData.isUserLoggedIn = membershipService.isUserLoggedIn();

            if ($scope.userData.isUserLoggedIn) {
                $scope.roleId = $rootScope.repository.loggedUser.roleId;
                $scope.email = $rootScope.repository.loggedUser.email;
                $scope.name = $rootScope.repository.loggedUser.name;
            }
        }

        function updateUserInfo(userFirstName, userEmail) {
            $scope.userData.isUserLoggedIn = membershipService.isUserLoggedIn();
            if ($scope.userData.isUserLoggedIn) {
                $rootScope.repository.loggedUser.email = userEmail;
                $rootScope.repository.loggedUser.name = userFirstName;
                $scope.email = userEmail;
                $scope.name = userFirstName;
            }
        }

        $scope.openLoginModal = function () {
            $modal.open({
                templateUrl: 'scripts/spa/account/login-modal.html',
                controller: 'loginModalCtrl',
                scope: $scope
            }).result.then(function ($scope) {
            }, function () {
            });
        }

        $scope.openRegisterModal = function () {
            $modal.open({
                templateUrl: 'scripts/spa/register/register-modal.html',
                controller: 'registerModalCtrl',
                scope: $scope
            }).result.then(function ($scope) {
            }, function () {
            });
        }

        $scope.userData.displayUserInfo();
    }

})(angular.module('walkyDoggy'));
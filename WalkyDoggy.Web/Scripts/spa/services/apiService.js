(function (app) {
    'use strict';

    app.factory('apiService', apiService);

    apiService.$inject = ['$http', '$location', 'notificationService', '$rootScope'];

    function apiService($http, $location, notificationService, $rootScope) {
        var service = {
            get: get,
            post: post,
            remove:remove
        };

        function get(url, config, success, failure) {
            return $http.get(url, config)
                    .then(function (result) {
                        success(result);
                    }, function (error) {
                        if (error.status == '401') {
                            notificationService.displayError('Authentication required.');
                            $rootScope.previousState = $location.path();
                            $location.path('/login');
                        }
                        else if (failure != null) {
                            failure(error);
                        }
                    });
        }

        function post(url, data, success, failure) {
            return $http.post(url, data)
                    .then(function (result) {
                        success(result);
                    }, function (error) {
                        if (error.status == '401') {
                            notificationService.displayError('Authentication required.');
                            $rootScope.previousState = $location.path();
                            $location.path('/login');
                        }
                        else if (failure != null) {
                            failure(error);
                        }
                        else if (error.status == '400') {
                            notificationService.displayError(error.data[0]);
                        }
                    });
        }
        function remove(url, success, failure) {
            return $http.delete(url).
                then(function (result) {
                    success(result);
                }, function (error) {
                    if (error.status == '401') {
                        notificationService.displayError('Debe autenticarse para ingresar a esta opción.');
                        $rootScope.previousState = $location.path();
                        $location.path('/login');
                    }
                    else if (failure != null) {
                        failure(error);
                    }
                    else {
                        if (error.data && error.data.length > 0 && error.data[0]) {
                            notificationService.displayError(error.data);
                        }
                        else {
                            notificationService.displayError("Se produjo un error inesperado.");
                        }
                    }
                });
        };

        return service;
    }

})(angular.module('common.core'));
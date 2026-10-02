(function (app) {
    'use strict';

    app.factory('servicioApi', servicioApi);

    servicioApi.$inject = ['$http', '$location', 'servicioNotificaciones', '$rootScope'];

    function servicioApi($http, $location, servicioNotificaciones, $rootScope) {
        var servicio = {
            get: get,
            post: post,
            remove:remove
        };

        function get(url, config, alExito, alFallar) {
            return $http.get(url, config)
                    .then(function (resultado) {
                        alExito(resultado);
                    }, function (error) {
                        if (error.status == '401') {
                            servicioNotificaciones.mostrarError('Authentication required.');
                            $rootScope.previousState = $location.path();
                            $location.path('/login');
                        }
                        else if (alFallar != null) {
                            alFallar(error);
                        }
                    });
        }

        function post(url, data, alExito, alFallar) {
            return $http.post(url, data)
                    .then(function (resultado) {
                        alExito(resultado);
                    }, function (error) {
                        if (error.status == '401') {
                            servicioNotificaciones.mostrarError('Authentication required.');
                            $rootScope.previousState = $location.path();
                            $location.path('/login');
                        }
                        else if (alFallar != null) {
                            alFallar(error);
                        }
                        else if (error.status == '400') {
                            servicioNotificaciones.mostrarError(error.data[0]);
                        }
                    });
        }
        function remove(url, alExito, alFallar) {
            return $http.delete(url).
                then(function (resultado) {
                    alExito(resultado);
                }, function (error) {
                    if (error.status == '401') {
                        servicioNotificaciones.mostrarError('Debe autenticarse para ingresar a esta opción.');
                        $rootScope.previousState = $location.path();
                        $location.path('/login');
                    }
                    else if (alFallar != null) {
                        alFallar(error);
                    }
                    else {
                        if (error.data && error.data.length > 0 && error.data[0]) {
                            servicioNotificaciones.mostrarError(error.data);
                        }
                        else {
                            servicioNotificaciones.mostrarError("Se produjo un error inesperado.");
                        }
                    }
                });
        };

        return servicio;
    }

})(angular.module('common.core'));
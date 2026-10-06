(function (app) {
    'use strict';

    app.factory('servicioApi', servicioApi);

    servicioApi.$inject = ['$http', '$location', 'servicioNotificaciones', '$rootScope', '$cookieStore'];

    function servicioApi($http, $location, servicioNotificaciones, $rootScope, $cookieStore) {
        var servicio = {
            get: get,
            post: post,
            remove:remove
        };

        //Un 401 con la sesion abierta significa que las credenciales dejaron de valer (por ejemplo, un administrador bloqueo la cuenta):
        //se cierra la sesion y se lleva a la persona a la portada, donde al iniciar sesion se le explica el motivo.
        //Sin sesion se mantiene el comportamiento anterior.
        function alNoAutenticado(mensajeSinSesion) {
            if ($rootScope.repository && $rootScope.repository.loggedUser) {
                $rootScope.repository = {};
                $cookieStore.remove('repository');
                $http.defaults.headers.common.Authorization = '';
                servicioNotificaciones.mostrarError('Tu sesión ya no es válida. Iniciá sesión de nuevo; si tu cuenta fue bloqueada, vas a ver el motivo.');
                $location.path('/public');
                return;
            }

            servicioNotificaciones.mostrarError(mensajeSinSesion);
            $rootScope.previousState = $location.path();
            $location.path('/login');
        }
        function get(url, config, alExito, alFallar) {
            return $http.get(url, config)
                    .then(function (resultado) {
                        alExito(resultado);
                    }, function (error) {
                        if (error.status == '401') {
                            alNoAutenticado('Authentication required.');
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
                            alNoAutenticado('Authentication required.');
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
                        alNoAutenticado('Debe autenticarse para ingresar a esta opción.');
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
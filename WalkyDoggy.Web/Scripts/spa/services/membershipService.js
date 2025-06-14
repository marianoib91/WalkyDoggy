(function (app) {
    'use strict';

    app.factory('membershipService', membershipService);

    membershipService.$inject = ['apiService', 'notificationService', '$http', '$base64', '$cookieStore', '$rootScope', '$location'];

    function membershipService(apiService, notificationService, $http, $base64, $cookieStore, $rootScope, $location) {
        var loggedUser = {};
        var membershipData = null;

        var service = {
            login: login,
            register: register,
            saveCredentials: saveCredentials,
            removeCredentials: removeCredentials,
            isUserLoggedIn: isUserLoggedIn
        }

        function login(user, completed) {
            apiService.post('/api/account/authenticate', user, completed, loginFailed);
        }

        function register(user, completed) {
            apiService.post('/api/account/register', user, completed, registrationFailed);
        }

        function saveCredentials(user, email) {
            loggedUser = user;
            membershipData = $base64.encode(user.email + ':' + user.password);
            if (user.id == 0) {
                var config = {
                    params: {
                        userId: user.userId
                    }
                }
            }
            else {
                var config = {
                    params: {
                        userId: user.id
                    }
                }
            }
           
            //Cliente
            if (user.roleId == '2') {
                apiService.get('/api/customers/getByUserId', config, onLoadUserCompleted);
            }

            //Paseador
            if (user.roleId == '3') {
                apiService.get('/api/walkers/getByUserId', config, onLoadUserCompleted);
            }

        }
        function onLoadUserCompleted(response) {


            if (loggedUser.roleId == '2') {
                $rootScope.repository = {
                    loggedUser: {
                        roleId: loggedUser.roleId,
                        id: response.data.userId,//se setea el userId obtenido en la validacion anterior
                        email: loggedUser.email,
                        authdata: membershipData,
                        walkerId: null,
                        customerId: response.data.id
                    }
                };
            }
            else if (loggedUser.roleId == '3') {
                $rootScope.repository = {
                    loggedUser: {
                        roleId: loggedUser.roleId,
                        id: response.data.userId,//se setea el userId obtenido en la validacion anterior
                        email: loggedUser.email,
                        authdata: membershipData,
                        walkerId: response.data.id,
                        customerId: null
                    }
                };
            }


            $http.defaults.headers.common['Authorization'] = 'Basic ' + membershipData;
            $cookieStore.put('repository', $rootScope.repository);           
            notificationService.displaySuccess('Bienvenido ' + response.data.email);
            $location.path('/');
        }

        function removeCredentials() {
            $rootScope.repository = {};
            $cookieStore.remove('repository');
            $http.defaults.headers.common.Authorization = '';
        };

        function loginFailed(response) {
            notificationService.displayError(response.data);
        }

        function registrationFailed(response) {

            notificationService.displayError('Imposible registrarse. Intente nuevamente');
        }

        function isUserLoggedIn() {
            return $rootScope.repository.loggedUser != null;
        }

        return service;
    }



})(angular.module('common.core'));
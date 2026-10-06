(function (app) {
    'use strict';

    app.factory('servicioMembresia', servicioMembresia);

    servicioMembresia.$inject = ['servicioApi', 'servicioNotificaciones', '$http', '$base64', '$cookieStore', '$rootScope', '$location'];

    function servicioMembresia(servicioApi, servicioNotificaciones, $http, $base64, $cookieStore, $rootScope, $location) {
        var loggedUser = {};
        var datosMembresia = null;
        var destinoPosterior = '/';

        var servicio = {
            iniciarSesion: iniciarSesion,
            registrar: registrar,
            guardarCredenciales: guardarCredenciales,
            quitarCredenciales: quitarCredenciales,
            haySesion: haySesion,
            actualizarContrasena: actualizarContrasena
        }

        function iniciarSesion(usuario, alCompletar) {
            servicioApi.post('/api/account/authenticate', usuario, alCompletar, falloInicioSesion);
        }

        function registrar(usuario, alCompletar) {
            servicioApi.post('/api/account/register', usuario, alCompletar, falloRegistro);
        }

        //destino: pantalla a la que se va despues de guardar las credenciales (por defecto, Mis paseos para el paseador
        //y Mis mascotas para el cliente, que es lo primero que necesita para pedir un paseo)
        function guardarCredenciales(usuario, email, destino) {
            loggedUser = usuario;
            destinoPosterior = destino || (usuario.roleId == '2' ? '/pets/list' : usuario.roleId == '1' ? '/admin' : '/');
            datosMembresia = $base64.encode(usuario.email + ':' + usuario.password);
            if (usuario.id == 0) {
                var config = {
                    params: {
                        userId: usuario.userId
                    }
                }
            }
            else {
                var config = {
                    params: {
                        userId: usuario.id
                    }
                }
            }
           
            //Cliente
            if (usuario.roleId == '2') {
                servicioApi.get('/api/customers/getByUserId', config, alCargarUsuario);
            }

            //Paseador
            if (usuario.roleId == '3') {
                servicioApi.get('/api/walkers/getByUserId', config, alCargarUsuario);
            }

            //Administrador: no tiene perfil de cliente ni de paseador, alcanza con los datos del login
            if (usuario.roleId == '1') {
                alCargarUsuario({ data: { userId: usuario.id, id: null, email: usuario.email } });
            }

        }
        function alCargarUsuario(respuesta) {


            if (loggedUser.roleId == '2') {
                $rootScope.repository = {
                    loggedUser: {
                        roleId: loggedUser.roleId,
                        id: respuesta.data.userId,//se setea el userId obtenido en la validacion anterior
                        email: loggedUser.email,
                        authdata: datosMembresia,
                        walkerId: null,
                        customerId: respuesta.data.id
                    }
                };
            }
            else if (loggedUser.roleId == '1') {
                $rootScope.repository = {
                    loggedUser: {
                        roleId: loggedUser.roleId,
                        id: respuesta.data.userId,
                        email: loggedUser.email,
                        authdata: datosMembresia,
                        walkerId: null,
                        customerId: null
                    }
                };
            }
            else if (loggedUser.roleId == '3') {
                $rootScope.repository = {
                    loggedUser: {
                        roleId: loggedUser.roleId,
                        id: respuesta.data.userId,//se setea el userId obtenido en la validacion anterior
                        email: loggedUser.email,
                        authdata: datosMembresia,
                        walkerId: respuesta.data.id,
                        customerId: null
                    }
                };
            }


            $http.defaults.headers.common['Authorization'] = 'Basic ' + datosMembresia;
            $cookieStore.put('repository', $rootScope.repository);           
            servicioNotificaciones.mostrarExito('Bienvenido ' + respuesta.data.email);
            $location.path(destinoPosterior);
        }

        //Despues de cambiar la contraseña la sesion abierta (y la cookie) tienen que usar la nueva
        function actualizarContrasena(contrasenaNueva) {
            datosMembresia = $base64.encode($rootScope.repository.loggedUser.email + ':' + contrasenaNueva);
            $rootScope.repository.loggedUser.authdata = datosMembresia;
            $http.defaults.headers.common['Authorization'] = 'Basic ' + datosMembresia;
            $cookieStore.put('repository', $rootScope.repository);
        }

        function quitarCredenciales() {
            $rootScope.repository = {};
            $cookieStore.remove('repository');
            $http.defaults.headers.common.Authorization = '';
        };

        function falloInicioSesion(respuesta) {
            servicioNotificaciones.mostrarError(respuesta.data);
        }

        function falloRegistro(respuesta) {

            servicioNotificaciones.mostrarError('Imposible registrarse. Intente nuevamente');
        }

        function haySesion() {
            return $rootScope.repository.loggedUser != null;
        }

        return servicio;
    }



})(angular.module('common.core'));
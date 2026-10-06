(function (app) {
    'use strict';

    app.controller('perfilCtrl', perfilCtrl);

    perfilCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', 'servicioApi', 'servicioSubidaArchivos', 'servicioConfirmacion', '$rootScope', '$location'];

    function perfilCtrl($scope, servicioMembresia, servicioNotificaciones, servicioApi, servicioSubidaArchivos, servicioConfirmacion, $rootScope, $location) {
        var idRolUsuario = $rootScope.repository.loggedUser.roleId;
        var idPaseador = $rootScope.repository.loggedUser.walkerId;
        $scope.usuario = null;

        //Cuenta de Mercado Pago del paseador (solo aplica al perfil de paseador)
        $scope.esPaseador = idRolUsuario == 3;
        $scope.mp = { loaded: false, configured: false, linked: false, userId: null, working: false };

        //2=Customer, 3=Walker
        var tipoEntidad = idRolUsuario == 2 ? 'customer' : 'walker';

        iniciar();

        $scope.alSeleccionarFoto = function ($files) {
            servicioSubidaArchivos.subirImagenDePerfil($files, tipoEntidad, $scope.usuario.id, function (profileImage) {
                $scope.usuario.profileImage = profileImage;
            });
        }

        function iniciar() {
            var emailUsuario = $rootScope.repository.loggedUser.email;
            var idUsuario = $rootScope.repository.loggedUser.id;

            var config = {
                params: {
                    userId: idUsuario
                }
            }

            //2=Customer
            if (idRolUsuario == 2) {
                servicioApi.get('/api/customers/getByUserId', config, alCargarUsuario);
            }
                //3=Walker
            else if (idRolUsuario == 3) {
                servicioApi.get('/api/walkers/getByUserId', config, alCargarUsuario);
               
            }



        }

    

        function alCargarUsuario(resultados) {
            $scope.usuario = resultados.data;
            $scope.usuario.phone = parseFloat($scope.usuario.phone, 10);
            $scope.usuario.streetNumber = parseFloat($scope.usuario.streetNumber, 10);

            if ($scope.esPaseador) {
                cargarEstadoMercadoPago();
                mostrarResultadoMercadoPago();
            }
        }

        /* ---------- Cobros con Mercado Pago (paseador) ---------- */

        function cargarEstadoMercadoPago() {
            servicioApi.get('/api/mercadopago/status', { params: { walkerId: idPaseador } }, function (resultado) {
                $scope.mp.configured = resultado.data.configured;
                $scope.mp.linked = resultado.data.linked;
                $scope.mp.userId = resultado.data.userId;
                $scope.mp.loaded = true;
            });
        }

        //Mercado Pago devuelve al paseador a "Mi perfil?mp=linked|denied|error" cuando termina de autorizar
        function mostrarResultadoMercadoPago() {
            var resultado = $location.search().mp;
            if (!resultado) {
                return;
            }

            if (resultado === 'linked') {
                servicioNotificaciones.mostrarExito('Vinculaste tu cuenta de Mercado Pago. Ya podés recibir pagos online.');
            } else if (resultado === 'denied') {
                servicioNotificaciones.mostrarError('No autorizaste la vinculación con Mercado Pago.');
            } else {
                servicioNotificaciones.mostrarError('No se pudo vincular la cuenta de Mercado Pago. Intentá nuevamente.');
            }

            $location.search({}).replace();
        }

        $scope.vincularMercadoPago = function () {
            $scope.mp.working = true;

            //Se le pide al servidor la direccion de autorizacion (lleva un "state" firmado) y se manda al paseador a Mercado Pago
            servicioApi.get('/api/mercadopago/authorizationUrl', { params: { walkerId: idPaseador } }, function (resultado) {
                window.location.href = resultado.data.url;
            }, function (error) {
                $scope.mp.working = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo iniciar la vinculación con Mercado Pago.');
            });
        };

        $scope.desvincularMercadoPago = function () {
            servicioConfirmacion.preguntar({
                title: '¿Desvincular Mercado Pago?',
                text: 'Tus clientes van a dejar de poder pagarte online y solo vas a poder cobrar en efectivo. Después podés volver a vincular tu cuenta.',
                confirmLabel: 'Desvincular',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                servicioApi.post('/api/mercadopago/unlink', { walkerId: idPaseador }, function () {
                    servicioNotificaciones.mostrarExito('Desvinculaste tu cuenta de Mercado Pago.');
                    cargarEstadoMercadoPago();
                }, function (error) {
                    servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo desvincular la cuenta.');
                });
            });
        };


        $scope.actualizarPerfil = function () {
            //Customer
            if (idRolUsuario == 2) {
                servicioApi.post('/api/customers/update', $scope.usuario, alActualizarUsuario);
            }
                //Walker
            else if (idRolUsuario == 3) {
                servicioApi.post('/api/walkers/update', $scope.usuario, alActualizarUsuario);
            }

        }

        function alActualizarUsuario(respuesta) {
            if (respuesta.status == 200) {
                servicioNotificaciones.mostrarExito('Perfil actualizado con éxito');

                //La barra superior muestra el nombre y la foto: se actualizan, y se vuelve al inicio del rol
                $rootScope.$broadcast('perfil:actualizado');
                $rootScope.irAlInicio();
            }
            else {
                servicioNotificaciones.mostrarError('No se pudo actualizar el perfil. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));

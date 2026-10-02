(function (app) {
    'use strict';

    app.controller('profileCtrl', profileCtrl);

    profileCtrl.$inject = ['$scope', 'membershipService', 'notificationService', 'apiService', 'fileUploadService', 'confirmService', '$rootScope', '$location'];

    function profileCtrl($scope, membershipService, notificationService, apiService, fileUploadService, confirmService, $rootScope, $location) {
        var userRoleId = $rootScope.repository.loggedUser.roleId;
        var walkerId = $rootScope.repository.loggedUser.walkerId;
        $scope.user = null;

        //Cuenta de Mercado Pago del paseador (solo aplica al perfil de paseador)
        $scope.isWalker = userRoleId == 3;
        $scope.mp = { loaded: false, configured: false, linked: false, userId: null, working: false };

        //2=Customer, 3=Walker
        var entityType = userRoleId == 2 ? 'customer' : 'walker';

        init();

        $scope.onPhotoSelected = function ($files) {
            fileUploadService.uploadProfileImage($files, entityType, $scope.user.id, function (profileImage) {
                $scope.user.profileImage = profileImage;
            });
        }

        function init() {
            var userEmail = $rootScope.repository.loggedUser.email;
            var userId = $rootScope.repository.loggedUser.id;

            var config = {
                params: {
                    userId: userId
                }
            }

            //2=Customer
            if (userRoleId == 2) {
                apiService.get('/api/customers/getByUserId', config, onLoadUserCompleted);
            }
                //3=Walker
            else if (userRoleId == 3) {
                apiService.get('/api/walkers/getByUserId', config, onLoadUserCompleted);
               
            }



        }

    

        function onLoadUserCompleted(results) {
            $scope.user = results.data;
            $scope.user.phone = parseFloat($scope.user.phone, 10);
            $scope.user.streetNumber = parseFloat($scope.user.streetNumber, 10);

            if ($scope.isWalker) {
                loadMercadoPagoStatus();
                showMercadoPagoResult();
            }
        }

        /* ---------- Cobros con Mercado Pago (paseador) ---------- */

        function loadMercadoPagoStatus() {
            apiService.get('/api/mercadopago/status', { params: { walkerId: walkerId } }, function (result) {
                $scope.mp.configured = result.data.configured;
                $scope.mp.linked = result.data.linked;
                $scope.mp.userId = result.data.userId;
                $scope.mp.loaded = true;
            });
        }

        //Mercado Pago devuelve al paseador a "Mi perfil?mp=linked|denied|error" cuando termina de autorizar
        function showMercadoPagoResult() {
            var result = $location.search().mp;
            if (!result) {
                return;
            }

            if (result === 'linked') {
                notificationService.displaySuccess('Vinculaste tu cuenta de Mercado Pago. Ya podés recibir pagos online.');
            } else if (result === 'denied') {
                notificationService.displayError('No autorizaste la vinculación con Mercado Pago.');
            } else {
                notificationService.displayError('No se pudo vincular la cuenta de Mercado Pago. Intentá nuevamente.');
            }

            $location.search({}).replace();
        }

        $scope.linkMercadoPago = function () {
            $scope.mp.working = true;

            //Se le pide al servidor la direccion de autorizacion (lleva un "state" firmado) y se manda al paseador a Mercado Pago
            apiService.get('/api/mercadopago/authorizationUrl', { params: { walkerId: walkerId } }, function (result) {
                window.location.href = result.data.url;
            }, function (error) {
                $scope.mp.working = false;
                notificationService.displayError(error.data && error.data[0] ? error.data[0] : 'No se pudo iniciar la vinculación con Mercado Pago.');
            });
        };

        $scope.unlinkMercadoPago = function () {
            confirmService.ask({
                title: '¿Desvincular Mercado Pago?',
                text: 'Tus clientes van a dejar de poder pagarte online y solo vas a poder cobrar en efectivo. Después podés volver a vincular tu cuenta.',
                confirmLabel: 'Desvincular',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                apiService.post('/api/mercadopago/unlink', { walkerId: walkerId }, function () {
                    notificationService.displaySuccess('Desvinculaste tu cuenta de Mercado Pago.');
                    loadMercadoPagoStatus();
                }, function (error) {
                    notificationService.displayError(error.data && error.data[0] ? error.data[0] : 'No se pudo desvincular la cuenta.');
                });
            });
        };


        $scope.updateProfile = function () {
            //Customer
            if (userRoleId == 2) {
                apiService.post('/api/customers/update', $scope.user, onUpdateUserCompleted);
            }
                //Walker
            else if (userRoleId == 3) {
                apiService.post('/api/walkers/update', $scope.user, onUpdateUserCompleted);
            }

        }

        function onUpdateUserCompleted(response) {
            if (response.status = 200) {
                notificationService.displaySuccess('Perfil actualizado con éxito');
                // $location.path('/');
                console.log();
            }
            else {
                notificationService.displayError('No se pudo actualizar el perfil. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));

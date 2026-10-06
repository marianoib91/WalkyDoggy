(function (app) {
    'use strict';

    app.controller('registroClienteCtrl', registroClienteCtrl);

    registroClienteCtrl.$inject = ['$scope', 'servicioApi', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location', '$window'];

    function registroClienteCtrl($scope, servicioApi, servicioMembresia, servicioNotificaciones, $rootScope, $location, $window) {

        //El registro se hace en dos pasos: datos personales y domicilio
        $scope.nombresPasos = ['Tus datos', 'Tu domicilio'];
        $scope.paso = 1;
        $scope.guardando = false;
        $scope.cliente = {};

        //Si se sale del registro (por ejemplo, con el boton Atras del navegador o yendo a otra pantalla) y se vuelve, se recupera lo cargado y el paso en que estaba.
        //Queda solo en memoria (no se guarda en el navegador) y se descarta al terminar el registro.
        var terminado = false;
        if ($rootScope.borradorRegistroCliente) {
            $scope.cliente = $rootScope.borradorRegistroCliente.cliente;
            $scope.paso = $rootScope.borradorRegistroCliente.paso;
        }
        $scope.$on('$destroy', function () {
            $rootScope.borradorRegistroCliente = terminado ? null : { cliente: $scope.cliente, paso: $scope.paso };
        });

        $scope.siguiente = function () {
            if ($scope.cliente.password != $scope.cliente.confirmPassword) {
                servicioNotificaciones.mostrarError('Las contraseñas no coinciden');
                return;
            }

            irAlPaso($scope.paso + 1);
        };

        $scope.volver = function () {
            irAlPaso($scope.paso - 1);
        };

        $scope.registrar = function () {
            if ($scope.guardando) {
                return;
            }

            if (!tieneDomicilio()) {
                servicioNotificaciones.mostrarError('Elegí tu domicilio de la lista de sugerencias o marcalo en el mapa.');
                return;
            }

            $scope.guardando = true;
            servicioApi.post('/api/customers/register', $scope.cliente, alRegistrar, function (error) {
                $scope.guardando = false;
                var mensaje = error.data && error.data[0] ? error.data[0] : 'No es posible completar la registración. Intente nuevamente.';
                servicioNotificaciones.mostrarError(mensaje);

                //Si el problema es el email, se vuelve al paso donde se carga
                if (/email/i.test(mensaje)) {
                    irAlPaso(1);
                }
            });
        };

        function alRegistrar(resultado) {
            if (resultado.status == 200) {
                terminado = true;

                //Al terminar el registro se lo lleva a una pantalla que lo invita a cargar a sus mascotas
                servicioMembresia.guardarCredenciales(resultado.data, $scope.cliente.firstName, '/customer-welcome');
            }
            else {
                $scope.guardando = false;
                servicioNotificaciones.mostrarError('No es posible completar la registración. Intente nuevamente.');
            }
        }

        function tieneDomicilio() {
            var cliente = $scope.cliente;
            return !!(cliente.cityId && cliente.streetName && cliente.streetNumber !== null && cliente.streetNumber !== undefined);
        }

        function irAlPaso(numero) {
            $scope.paso = Math.max(1, Math.min(2, numero));
            $window.scrollTo(0, 0);
        }
    }

})(angular.module('common.core'));

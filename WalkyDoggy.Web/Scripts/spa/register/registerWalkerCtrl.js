(function (app) {
    'use strict';

    app.controller('registroPaseadorCtrl', registroPaseadorCtrl);

    registroPaseadorCtrl.$inject = ['$scope', 'servicioApi', 'servicioMembresia', 'servicioNotificaciones', '$rootScope', '$location', '$window'];

    function registroPaseadorCtrl($scope, servicioApi, servicioMembresia, servicioNotificaciones, $rootScope, $location, $window) {

        //El registro se hace en tres pasos: datos personales, zona de trabajo y tarifa/cobro
        $scope.nombresPasos = ['Tus datos', 'Dónde trabajás', 'Tarifa y cobro'];
        $scope.paso = 1;
        $scope.guardando = false;

        //El radio arranca en 5 km y la cantidad de perros en 3; la direccion de referencia se completa en el paso 2
        $scope.paseador = { serviceRadiusKm: 5, maxPetsAtOnce: 3 };
        $scope.cantidadesDePerros = [1, 2, 3, 4, 5];

        //Si se sale del registro (por ejemplo, con el boton Atras del navegador o yendo a otra pantalla) y se vuelve, se recupera lo cargado y el paso en que estaba.
        //Queda solo en memoria (no se guarda en el navegador) y se descarta al terminar el registro.
        var terminado = false;
        if ($rootScope.borradorRegistroPaseador) {
            $scope.paseador = $rootScope.borradorRegistroPaseador.paseador;
            $scope.paso = $rootScope.borradorRegistroPaseador.paso;
        }
        $scope.$on('$destroy', function () {
            $rootScope.borradorRegistroPaseador = terminado ? null : { paseador: $scope.paseador, paso: $scope.paso };
        });

        $scope.siguiente = function () {
            if ($scope.paso === 1 && $scope.paseador.password != $scope.paseador.confirmPassword) {
                servicioNotificaciones.mostrarError('Las contraseñas no coinciden');
                return;
            }

            if ($scope.paso === 2 && !tieneUbicacion()) {
                servicioNotificaciones.mostrarError('Elegí la dirección de referencia de la lista de sugerencias o marcala en el mapa.');
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
            $scope.guardando = true;

            servicioApi.post('/api/walkers/register', $scope.paseador, alRegistrar, function (error) {
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
                //Al terminar el registro se lo lleva a la pantalla de bienvenida
                servicioMembresia.guardarCredenciales(resultado.data, $scope.paseador.firstName, '/walker-welcome');
            }
            else {
                $scope.guardando = false;
                servicioNotificaciones.mostrarError('No es posible completar la registración. Intente nuevamente.');
            }
        }

        function tieneUbicacion() {
            var paseador = $scope.paseador;
            return !!(paseador.cityId && paseador.streetName && paseador.streetNumber !== null && paseador.streetNumber !== undefined && paseador.latitude && paseador.longitude);
        }

        function irAlPaso(numero) {
            $scope.paso = Math.max(1, Math.min(3, numero));
            $window.scrollTo(0, 0);
        }
    }

})(angular.module('common.core'));

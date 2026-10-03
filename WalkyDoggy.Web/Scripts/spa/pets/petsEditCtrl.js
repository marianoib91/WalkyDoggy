(function (app) {
    'use strict';

    app.controller('editarMascotaCtrl', editarMascotaCtrl);

    editarMascotaCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', 'servicioApi', 'servicioSubidaArchivos', 'servicioCaracteristicas', '$rootScope', '$location', '$routeParams'];

    function editarMascotaCtrl($scope, servicioMembresia, servicioNotificaciones, servicioApi, servicioSubidaArchivos, servicioCaracteristicas, $rootScope, $location, $routeParams) {

        $scope.idMascota = $routeParams.id;
        $scope.idUsuario = $rootScope.repository.loggedUser.id;
        $scope.idCliente = $rootScope.repository.loggedUser.customerId;

        $scope.mascota = {};
        $scope.razas = {};
        $scope.tamanos = {};

        //Caracteristicas (pares de opuestos): las elegidas se guardan en la mascota como codigos separados por coma
        $scope.pares = servicioCaracteristicas.pares;
        $scope.minimoEnComun = servicioCaracteristicas.minimoEnComun;
        $scope.rasgosElegidos = [];

        $scope.esRasgo = function (codigo) {
            return $scope.rasgosElegidos.indexOf(codigo) !== -1;
        };

        $scope.cantidadRasgos = function () {
            return $scope.rasgosElegidos.length;
        };

        //Tocar una caracteristica la elige (y saca la opuesta del par); tocarla de nuevo la saca
        $scope.alternarRasgo = function (par, codigo) {
            var yaElegida = $scope.esRasgo(codigo);

            $scope.rasgosElegidos = $scope.rasgosElegidos.filter(function (elegido) {
                return !par.some(function (rasgo) { return rasgo.codigo === elegido; });
            });
            if (!yaElegida) {
                $scope.rasgosElegidos.push(codigo);
            }
        };
        var fotoPendiente = null;
        iniciar();

        $scope.imagenPrevia = null;

        $scope.alSeleccionarFoto = function ($files) {
            if (!$files || !$files.length) {
                return;
            }

            if ($scope.imagenPrevia) {
                URL.revokeObjectURL($scope.imagenPrevia);
            }
            $scope.imagenPrevia = URL.createObjectURL($files[0]);

            if ($scope.mascota.id) {
                servicioSubidaArchivos.subirImagenDePerfil($files, 'pet', $scope.mascota.id, function (profileImage) {
                    $scope.mascota.profileImage = profileImage;
                });
            } else {
                fotoPendiente = $files;
            }
        }

        function iniciar() {
        var config = {
                params: {
                    id: $scope.idMascota
                }
            }
            servicioApi.get('/api/pets/getById', config, alCargarMascota);
            //Traer las razas y los tamaños de la BD
            servicioApi.get('/api/breeds/getAll', null, alCargarRazas);
            servicioApi.get('/api/sizes/getAll', null, alCargarTamanos);

        }

        function alCargarCliente(resultado) {
            $scope.idCliente = resultado.data.id;
            
        }

        function alCargarMascota(resultado) {
            $scope.mascota = resultado.data;
            $scope.rasgosElegidos = servicioCaracteristicas.leer($scope.mascota && $scope.mascota.traits);
        }

        function alCargarRazas(resultado) {
            $scope.razas = resultado.data;
        }

        function alCargarTamanos(resultado) {
            $scope.tamanos = resultado.data;
        }

        $scope.actualizarMascota = function () {
            $scope.mascota.traits = $scope.rasgosElegidos.join(',');

            if ($scope.mascota.id) {
                servicioApi.post('/api/pets/update', $scope.mascota, alActualizarMascota);
            }
            else {
                $scope.mascota.customerId = $scope.idCliente;
                servicioApi.post('/api/pets/register', $scope.mascota, alRegistrarMascota);
            }
        }
        function alRegistrarMascota(respuesta) {
            if (respuesta.status == 200) {
                servicioNotificaciones.mostrarExito(respuesta.data.name + ' se ha registrado exitosamente.');
                if (fotoPendiente) {
                    servicioSubidaArchivos.subirImagenDePerfil(fotoPendiente, 'pet', respuesta.data.id, function () {
                        $location.path('/pets/list');
                    });
                } else {
                    $location.path('/pets/list');
                }
            }
            else {
                servicioNotificaciones.mostrarError('No se pudo registrar a tu mascota. Intente nuevamente');
            }
        }
        function alActualizarMascota(respuesta) {
            if (respuesta.status == 200) {
                servicioNotificaciones.mostrarExito('Cambios guardados con éxito');
                $location.path('/pets/list');
            }
            else {
                servicioNotificaciones.mostrarError('No se pudieron guardar los cambios realizados. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
(function (app) {
    'use strict';

    app.controller('listaMascotasCtrl', listaMascotasCtrl);

    listaMascotasCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', 'servicioApi', '$rootScope', '$location', 'sweetAlert'];

    function listaMascotasCtrl($scope, servicioMembresia, servicioNotificaciones, servicioApi, $rootScope, $location, sweetAlert) {

        //Se obtiene el id del cliente guardado en el indexCtrl
        $scope.idUsuario = $rootScope.repository.loggedUser.id;
        $scope.idCliente = $rootScope.repository.loggedUser.customerId;
        $scope.mascotas = {};

        iniciar();

        function iniciar() {
            var config = {
                params: {
                    customerId: $scope.idCliente
                }
            }
            servicioApi.get('/api/pets/getAllByCustomerId/', config, alCargarMascotas);
        }

        function alCargarMascotas(resultado) {
            $scope.mascotas = resultado.data;
        }

        $scope.editar = function (id) {
            $location.path('/pets/edit/' + id);
        }

        $scope.eliminar = function (id) {
            sweetAlert.swal({
                title: "Eliminar mascota",
                text: "¿Desea eliminar la mascota seleccionada?",
                type: "error",
                showCancelButton: true,
                confirmButtonText: "Aceptar",
                cancelButtonText: "Cancelar"
            }).then(function (confirmo) {
                if (confirmo) {
                    var r = true;
                }
                if (r == true) {
                    servicioApi.remove('/api/pets/' + id, alEliminarMascota);
                }
            });

        }
        function alEliminarMascota(respuesta) {
            if (respuesta.status == 200) {
                servicioNotificaciones.mostrarExito('Tu mascota se ha eliminado con éxito');
                var config = {
                    params: {
                        customerId: $scope.idCliente
                    }
                }
                servicioApi.get('/api/pets/getAllByCustomerId/', config, alCargarMascotas);
            }
            else {
                servicioNotificaciones.mostrarError('No se pudo eliminar tu mascota. Intente nuevamente');
            }
        }
    }

})(angular.module('common.core'));
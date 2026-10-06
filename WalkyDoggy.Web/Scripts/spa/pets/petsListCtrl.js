(function (app) {
    'use strict';

    app.controller('listaMascotasCtrl', listaMascotasCtrl);

    listaMascotasCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', 'servicioApi', 'servicioCaracteristicas', '$rootScope', '$location', 'sweetAlert', 'servicioSugerencia'];

    function listaMascotasCtrl($scope, servicioMembresia, servicioNotificaciones, servicioApi, servicioCaracteristicas, $rootScope, $location, sweetAlert, servicioSugerencia) {

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

            //El domicilio del cliente (para mostrarle los avisos de los comercios que tiene cerca)
            servicioApi.get('/api/customers/getByUserId', { params: { userId: $scope.idUsuario } }, function (resultado) {
                $scope.cliente = resultado.data;
            });

            //Si en su historial hay un dia y horario que se repite, se le sugiere reservarlo (sin patron claro no aparece nada)
            servicioSugerencia.cargar($scope.idCliente, function (sugerencia) {
                $scope.sugerencia = sugerencia;
            });
        }

        /* ---------- Sugerencia de reserva ---------- */

        $scope.botonSugerencia = 'Buscar paseador';
        $scope.textoSugerencia = servicioSugerencia.texto;
        $scope.evidenciaSugerencia = servicioSugerencia.evidencia;

        //Lleva a la busqueda con el proximo dia que corresponde, el horario y las mascotas ya elegidos
        $scope.reservarSugerencia = function (sugerencia) {
            $rootScope.busquedaPaseo = {
                idCliente: $scope.idCliente,
                retiro: { modo: 'home', otro: {} },
                busqueda: { fecha: moment(sugerencia.nextDate, 'YYYY-MM-DD').toDate(), hora: sugerencia.time },
                idsMascotas: sugerencia.petIds
            };
            $location.path('/');
        };

        $scope.descartarSugerencia = function (sugerencia) {
            servicioSugerencia.descartar($scope.idCliente, sugerencia);
            $scope.sugerencia = null;
        };

        function alCargarMascotas(resultado) {
            $scope.mascotas = resultado.data;
        }

        //Las caracteristicas de la mascota en castellano (["Juguetón", "Corredor"])
        $scope.rasgosDe = function (mascota) {
            return servicioCaracteristicas.leer(mascota.traits).map(servicioCaracteristicas.textoDe);
        };

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

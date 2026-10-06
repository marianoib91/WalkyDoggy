(function (app) {
    'use strict';

    app.controller('listaMascotasCtrl', listaMascotasCtrl);

    listaMascotasCtrl.$inject = ['$scope', 'servicioMembresia', 'servicioNotificaciones', 'servicioApi', 'servicioCaracteristicas', '$rootScope', '$location', 'sweetAlert'];

    function listaMascotasCtrl($scope, servicioMembresia, servicioNotificaciones, servicioApi, servicioCaracteristicas, $rootScope, $location, sweetAlert) {

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

            //Si en su historial hay un dia y horario que se repite, se le sugiere reservarlo (204 = sin patron claro)
            servicioApi.get('/api/predictions/nextBooking', { params: { customerId: $scope.idCliente } }, function (resultado) {
                if (resultado.status === 200 && resultado.data && !sugerenciaDescartada(resultado.data)) {
                    $scope.sugerencia = resultado.data;
                }
            });
        }

        /* ---------- Sugerencia de reserva ---------- */

        var DIAS = ['domingos', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábados'];

        function claveDescarte(sugerencia) {
            return 'wd-sugerencia-' + $scope.idCliente + '-' + sugerencia.nextDate;
        }

        function sugerenciaDescartada(sugerencia) {
            try {
                return !!window.localStorage.getItem(claveDescarte(sugerencia));
            } catch (e) {
                return false;
            }
        }

        //"Rex y Luna" / "Rex, Luna y Lola"
        function unir(nombres) {
            return nombres.length > 1 ? nombres.slice(0, -1).join(', ') + ' y ' + nombres[nombres.length - 1] : nombres[0];
        }

        $scope.diaSugerido = function (sugerencia) {
            return DIAS[sugerencia.dayOfWeek];
        };

        $scope.textoSugerencia = function (sugerencia) {
            var verbo = sugerencia.petNames.length > 1 ? 'suelen' : 'suele';
            return unir(sugerencia.petNames) + ' ' + verbo + ' salir los ' + DIAS[sugerencia.dayOfWeek] + ' a las ' + sugerencia.time + '.';
        };

        $scope.evidenciaSugerencia = function (sugerencia) {
            return 'Se ve en tus últimas ' + sugerencia.total + ' reservas: ' + sugerencia.matches + ' fueron los ' + DIAS[sugerencia.dayOfWeek] + '.';
        };

        //Lleva a la busqueda con el proximo dia que corresponde, el horario y las mascotas ya elegidos
        $scope.reservarSugerencia = function (sugerencia) {
            var fecha = moment(sugerencia.nextDate, 'YYYY-MM-DD').toDate();
            $rootScope.busquedaPaseo = {
                idCliente: $scope.idCliente,
                retiro: { modo: 'home', otro: {} },
                busqueda: { fecha: fecha, hora: sugerencia.time },
                idsMascotas: sugerencia.petIds
            };
            $location.path('/');
        };

        //"Ahora no": no se vuelve a mostrar para esa fecha
        $scope.descartarSugerencia = function (sugerencia) {
            try {
                window.localStorage.setItem(claveDescarte(sugerencia), '1');
            } catch (e) { }
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
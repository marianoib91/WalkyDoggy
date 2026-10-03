(function (app) {
    'use strict';

    app.controller('perfilPaseadorCtrl', perfilPaseadorCtrl);

    perfilPaseadorCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioFavoritos', '$routeParams', '$rootScope'];

    function perfilPaseadorCtrl($scope, servicioApi, servicioNotificaciones, servicioFavoritos, $routeParams, $rootScope) {
        var tamanoPagina = 8;
        var dias = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

        $scope.idPaseador = Number($routeParams.id);
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.esCliente = $scope.roleId == 2;
        $scope.esPerfilPropio = $scope.roleId == 3 && $rootScope.repository.loggedUser.walkerId == $scope.idPaseador;

        $scope.paseador = null;
        $scope.noEncontrado = false;
        $scope.disponibilidad = [];
        $scope.resumen = null;
        $scope.valoraciones = [];
        $scope.total = 0;
        $scope.hayMas = false;
        $scope.cargandoValoraciones = false;
        $scope.filter = { sort: 'recent', stars: '' };

        var pagina = 1;

        iniciar();

        $scope.esFavorito = function () {
            return servicioFavoritos.esFavorito($scope.idPaseador);
        };

        $scope.alternarFavorito = function () {
            servicioFavoritos.alternar($rootScope.repository.loggedUser.customerId, $scope.idPaseador);
        };

        function iniciar() {
            if ($scope.esCliente) {
                servicioFavoritos.cargar($rootScope.repository.loggedUser.customerId);
            }

            servicioApi.get('/api/walkers/getDetail', { params: { id: $scope.idPaseador } }, function (resultado) {
                if (!resultado.data) {
                    $scope.noEncontrado = true;
                    return;
                }
                $scope.paseador = resultado.data;
            });

            servicioApi.get('/api/workDays/getAllByWalkerId', { params: { walkerId: $scope.idPaseador } }, function (resultado) {
                $scope.disponibilidad = armarDisponibilidad(resultado.data);
            });

            servicioApi.get('/api/ratings/summary', { params: { walkerId: $scope.idPaseador } }, function (resultado) {
                $scope.resumen = resultado.data;
            });

            cargarValoraciones(true);
        }

        //Un renglon por dia con sus franjas: "08:00 a 14:00 · 16:00 a 20:00" o "Todo el día"
        function armarDisponibilidad(jornadas) {
            return dias.map(function (dia) {
                var franjas = jornadas.filter(function (jornada) { return jornada.dayOfWeek === dia; })
                                     .sort(function (a, b) { return a.timeFrom < b.timeFrom ? -1 : 1; });

                var texto = franjas.map(function (range) {
                    return range.timeFrom === '00:00' && range.timeUntil === '24:00'
                        ? 'Todo el día'
                        : range.timeFrom + ' a ' + range.timeUntil;
                }).join(' · ');

                return { day: dia, works: franjas.length > 0, text: texto };
            });
        }

        function cargarValoraciones(reiniciar) {
            if (reiniciar) {
                pagina = 1;
            }

            $scope.cargandoValoraciones = true;
            var parametros = { walkerId: $scope.idPaseador, sort: $scope.filter.sort, page: pagina, pageSize: tamanoPagina };
            if ($scope.filter.stars) {
                parametros.stars = $scope.filter.stars;
            }

            servicioApi.get('/api/ratings/list', { params: parametros }, function (resultado) {
                $scope.valoraciones = reiniciar ? resultado.data.items : $scope.valoraciones.concat(resultado.data.items);
                $scope.total = resultado.data.total;
                $scope.hayMas = resultado.data.hasMore;
                $scope.cargandoValoraciones = false;
            }, function () {
                $scope.cargandoValoraciones = false;
                servicioNotificaciones.mostrarError('No se pudieron cargar las opiniones.');
            });
        }

        $scope.filtrarDeNuevo = function () {
            cargarValoraciones(true);
        };

        //Lleva a la seccion de opiniones (el enlace "N calificaciones" de la cabecera)
        $scope.irAOpiniones = function () {
            var seccion = document.getElementById('opiniones');
            if (seccion && seccion.scrollIntoView) {
                seccion.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        };

        $scope.cargarMas = function () {
            pagina++;
            cargarValoraciones(false);
        };

        $scope.formatearPromedio = function (valor) {
            return valor === null || valor === undefined ? '' : Number(valor).toFixed(1).replace('.', ',');
        };

        //Ancho de la barra de cada cantidad de estrellas, en proporcion al total de valoraciones
        $scope.anchoBarra = function (grupo) {
            return $scope.resumen && $scope.resumen.count > 0 ? Math.round(grupo.count * 100 / $scope.resumen.count) : 0;
        };

        $scope.textoFecha = function (fecha) {
            return moment(fecha).format('DD/MM/YYYY');
        };

        $scope.textoCantidad = function (cantidad, singular, plural) {
            return cantidad + ' ' + (cantidad === 1 ? singular : plural);
        };
    }

})(angular.module('walkyDoggy'));

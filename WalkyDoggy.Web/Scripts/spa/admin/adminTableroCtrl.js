(function (app) {
    'use strict';

    app.controller('adminTableroCtrl', adminTableroCtrl);

    adminTableroCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones'];

    //Tablero de metricas: reservas, cancelaciones, cobros, valoraciones y denuncias de un periodo
    function adminTableroCtrl($scope, servicioApi, servicioNotificaciones) {
        var textosEstado = {
            Pending: 'Esperando al paseador',
            Upcoming: 'Confirmadas por empezar',
            InProgress: 'En curso',
            ToCollect: 'Terminadas, falta cobrar',
            Collected: 'Cobradas',
            Cancelled: 'Canceladas'
        };
        var textosCancelo = { Customer: 'El cliente', Walker: 'El paseador', System: 'Sin respuesta del paseador', Admin: 'Un administrador' };

        //Periodos que se ofrecen (en dias, terminando hoy); "custom" usa las fechas elegidas
        $scope.periodos = [
            { valor: '7', texto: 'Últimos 7 días' },
            { valor: '30', texto: 'Últimos 30 días' },
            { valor: '90', texto: 'Últimos 90 días' },
            { valor: 'custom', texto: 'Entre fechas...' }
        ];
        $scope.filtro = { periodo: '30', desde: '', hasta: '' };
        $scope.tablero = null;
        $scope.cargando = false;

        function formatoFecha(fecha) {
            return moment(fecha).format('YYYY-MM-DD');
        }

        function cargar() {
            var parametros = {};

            if ($scope.filtro.periodo === 'custom') {
                if (!$scope.filtro.desde || !$scope.filtro.hasta) {
                    return;
                }
                parametros.from = formatoFecha($scope.filtro.desde);
                parametros.to = formatoFecha($scope.filtro.hasta);
            } else {
                var dias = Number($scope.filtro.periodo);
                parametros.to = moment().format('YYYY-MM-DD');
                parametros.from = moment().subtract(dias - 1, 'days').format('YYYY-MM-DD');
            }

            $scope.cargando = true;
            servicioApi.get('/api/admin/dashboard', { params: parametros }, function (resultado) {
                $scope.tablero = resultado.data;
                $scope.cargando = false;
            }, function (error) {
                $scope.cargando = false;
                servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : 'No se pudo cargar el tablero.');
            });
        }

        $scope.cambiarPeriodo = cargar;
        $scope.aplicarFechas = cargar;

        $scope.textoEstado = function (clave) { return textosEstado[clave] || clave; };
        $scope.textoCancelo = function (clave) { return textosCancelo[clave] || clave; };

        //Ancho (en %) de una barra respecto de la mayor de su grupo
        $scope.anchoBarra = function (cantidad, grupo) {
            var maximo = 0;
            angular.forEach(grupo, function (item) { maximo = Math.max(maximo, item.count); });
            return maximo === 0 ? 0 : Math.round(100 * cantidad / maximo);
        };

        //Altura (en %) de la barra de un dia del grafico respecto del dia con mas reservas
        $scope.alturaDia = function (dia) {
            var maximo = 0;
            angular.forEach($scope.tablero.byDay, function (d) { maximo = Math.max(maximo, d.total); });
            return maximo === 0 ? 0 : Math.max(4, Math.round(100 * dia.total / maximo));
        };

        //La parte cancelada de la barra de un dia (en % de la barra)
        $scope.parteCancelada = function (dia) {
            return dia.total === 0 ? 0 : Math.round(100 * dia.cancelled / dia.total);
        };

        $scope.textoDia = function (dia) {
            return moment(dia.Date || dia.date).format('DD/MM') + ': ' + dia.total + (dia.total === 1 ? ' reserva' : ' reservas') +
                (dia.cancelled > 0 ? ' (' + dia.cancelled + ' cancelada' + (dia.cancelled === 1 ? '' : 's') + ')' : '');
        };

        //Marcas del eje: la primera, la del medio y la ultima fecha del grafico
        $scope.marcasDelGrafico = function () {
            var dias = $scope.tablero.byDay;
            if (!dias.length) { return []; }
            return [dias[0], dias[Math.floor(dias.length / 2)], dias[dias.length - 1]].map(function (d) { return moment(d.date).format('DD/MM'); });
        };

        $scope.textoPeriodo = function () {
            return moment($scope.tablero.from).format('DD/MM/YYYY') + ' al ' + moment($scope.tablero.to).format('DD/MM/YYYY');
        };

        cargar();
    }

})(angular.module('walkyDoggy'));

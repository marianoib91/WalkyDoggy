(function (app) {
    'use strict';

    app.controller('adminPaseosCtrl', adminPaseosCtrl);

    adminPaseosCtrl.$inject = ['$scope', '$timeout', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion'];

    //Todas las reservas del sistema, con la posibilidad de cancelar una en un caso excepcional
    function adminPaseosCtrl($scope, $timeout, servicioApi, servicioNotificaciones, servicioConfirmacion) {
        var tamanoPagina = 15;
        var textosEstado = {
            Pending: 'Esperando al paseador',
            Upcoming: 'Confirmado',
            InProgress: 'En curso',
            ToCollect: 'Para cobrar',
            Collected: 'Cobrado',
            Cancelled: 'Cancelado'
        };
        var textosCancelo = { Customer: 'por el cliente', Walker: 'por el paseador', System: 'sin respuesta del paseador', Admin: 'por un administrador' };
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

        $scope.filtros = { estado: '', texto: '', desde: '', hasta: '' };
        $scope.paseos = [];
        $scope.total = 0;
        $scope.pagina = 1;
        $scope.cargando = false;

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        function cargar(pagina) {
            $scope.cargando = true;
            var parametros = { page: pagina, pageSize: tamanoPagina };
            if ($scope.filtros.estado) { parametros.status = $scope.filtros.estado; }
            if ($scope.filtros.texto) { parametros.search = $scope.filtros.texto; }
            if ($scope.filtros.desde) { parametros.from = moment($scope.filtros.desde).format('YYYY-MM-DD'); }
            if ($scope.filtros.hasta) { parametros.to = moment($scope.filtros.hasta).format('YYYY-MM-DD'); }

            servicioApi.get('/api/admin/walks', { params: parametros }, function (resultado) {
                $scope.paseos = resultado.data.walks;
                $scope.total = resultado.data.total;
                $scope.pagina = resultado.data.page;
                $scope.cargando = false;
            }, function (error) {
                $scope.cargando = false;
                mostrarError(error, 'No se pudieron cargar los paseos.');
            });
        }

        $scope.buscar = function () { cargar(1); };

        var espera = null;
        $scope.alEscribir = function () {
            $timeout.cancel(espera);
            espera = $timeout(function () { cargar(1); }, 350);
        };
        $scope.$on('$destroy', function () { $timeout.cancel(espera); });

        $scope.hayAnterior = function () { return $scope.pagina > 1; };
        $scope.haySiguiente = function () { return $scope.pagina * tamanoPagina < $scope.total; };
        $scope.irA = function (pagina) { cargar(pagina); };

        $scope.textoEstado = function (paseo) { return textosEstado[paseo.status] || paseo.status; };
        $scope.textoCancelo = function (paseo) { return textosCancelo[paseo.cancelledBy] || ''; };
        $scope.claseEstado = function (paseo) {
            return paseo.status === 'Collected' ? 'wd-status-confirmed' : paseo.status === 'Cancelled' ? 'wd-status-cancelled' : 'wd-status-pending';
        };

        $scope.textoFecha = function (paseo) {
            var fecha = moment(paseo.date);
            return nombresDias[fecha.day()] + ' ' + fecha.format('DD/MM/YYYY') + ', ' + paseo.timeFrom;
        };

        $scope.textoPago = function (paseo) {
            var metodo = paseo.paymentMethod === 'MercadoPago' ? 'Mercado Pago' : 'Efectivo';
            var estado = paseo.paymentStatus === 'Received' ? 'cobrado' : paseo.paymentStatus === 'Paid' ? 'pagado' : 'sin cobrar';
            return metodo + ' · ' + estado;
        };

        $scope.cancelar = function (paseo) {
            var aviso = paseo.status === 'Pending'
                ? 'Es una solicitud que el paseador todavía no respondió. '
                : paseo.status === 'InProgress'
                    ? 'El paseo ya empezó. '
                    : '';

            servicioConfirmacion.preguntar({
                title: '¿Cancelar la reserva de ' + paseo.customerName + ' con ' + paseo.walkerName + '?',
                text: aviso + 'Es una medida excepcional: se avisa por mail al cliente y al paseador, con tu motivo, y el horario queda libre. No se puede deshacer.',
                input: { label: 'Motivo de la cancelación', placeholder: 'Por ejemplo: pedido de ambas partes por un error de carga' },
                confirmLabel: 'Cancelar reserva',
                cancelLabel: 'Volver',
                danger: true
            }).then(function (motivo) {
                servicioApi.post('/api/admin/walks/cancel', { bookingKey: paseo.bookingKey, reason: motivo }, function () {
                    servicioNotificaciones.mostrarExito('Cancelaste la reserva.');
                    cargar($scope.pagina);
                }, function (error) { mostrarError(error, 'No se pudo cancelar la reserva.'); });
            });
        };

        cargar(1);
    }

})(angular.module('walkyDoggy'));

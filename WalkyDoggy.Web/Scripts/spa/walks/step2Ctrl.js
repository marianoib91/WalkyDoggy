(function (app) {
    'use strict';

    app.controller('paso2Ctrl', paso2Ctrl);

    paso2Ctrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', '$rootScope', '$location'];

    function paso2Ctrl($scope, servicioApi, servicioNotificaciones, $rootScope, $location) {
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var borrador = $rootScope.borradorPaseo;

        //Si se llega sin haber elegido dia y horario en el paso 1 se vuelve al listado de paseadores
        if (!borrador) {
            $location.search({}).path('/');
            return;
        }

        $scope.borrador = borrador;
        $scope.paseador = null;
        $scope.detalles = '';
        $scope.guardando = false;
        $scope.pago = { method: 'Cash' };

        //Mercado Pago solo se ofrece si el paseador vinculo su cuenta (se sabe al cargar su detalle)
        $scope.mpHabilitado = false;

        var fechaPaseo = moment(borrador.date, 'YYYY-MM-DD');
        $scope.textoFecha = nombresDias[fechaPaseo.day()] + ' ' + fechaPaseo.format('DD/MM/YYYY');
        $scope.textoHorario = borrador.timeFrom + ' a ' + (Number(borrador.timeFrom.split(':')[0]) + 1) + ':00';
        $scope.nombresMascotas = borrador.pets.map(function (mascota) { return mascota.name; }).join(', ');

        var retiro = borrador.pickup;
        var lineaRetiro = [retiro.streetName, retiro.streetNumber].filter(Boolean).join(' ');
        $scope.textoRetiro = [lineaRetiro, retiro.cityName, retiro.provinceName].filter(Boolean).join(', ');
        $scope.retiraEnDomicilio = retiro.isHome;

        servicioApi.get('/api/walkers/getDetail', { params: { id: borrador.walkerId } }, function (resultado) {
            $scope.paseador = resultado.data;
            $scope.mpHabilitado = !!resultado.data.mercadoPagoLinked;
            if (!$scope.mpHabilitado && $scope.pago.method === 'MercadoPago') {
                $scope.pago.method = 'Cash';
            }
        });

        $scope.total = function () {
            return $scope.paseador ? $scope.paseador.amount * borrador.pets.length : 0;
        };

        $scope.enviar = function () {
            if ($scope.guardando) {
                return;
            }
            $scope.guardando = true;

            var pedido = {
                walkerId: borrador.walkerId,
                date: borrador.date,
                timeFrom: borrador.timeFrom,
                details: $scope.detalles,
                paymentMethod: $scope.pago.method,
                petIds: borrador.pets.map(function (mascota) { return mascota.id; })
            };

            //Si retira en el domicilio no se manda nada: el servidor usa (y guarda) el domicilio del cliente
            if (!retiro.isHome) {
                pedido.pickupStreetName = retiro.streetName;
                pedido.pickupStreetNumber = retiro.streetNumber;
                pedido.pickupCityId = retiro.cityId;
                pedido.pickupLatitude = retiro.latitude;
                pedido.pickupLongitude = retiro.longitude;
            }

            servicioApi.post('/api/walks/register', pedido, function () {
                servicioNotificaciones.mostrarExito('Solicitud enviada. Cuando el paseador confirme el paseo lo vas a ver en "Paseos solicitados".');
                $rootScope.borradorPaseo = null;
                $location.search({}).path('/walks/requested');
            }, function (error) {
                $scope.guardando = false;
                var mensaje = error.data && error.data[0] ? error.data[0] : 'No se pudo solicitar el paseo. Intente nuevamente.';
                servicioNotificaciones.mostrarError(mensaje);
            });
        };
    }

})(angular.module('walkyDoggy'));

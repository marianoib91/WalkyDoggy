(function (app) {
    'use strict';

    app.directive('wdPublicidad', wdPublicidad);

    wdPublicidad.$inject = ['$http', '$window', '$timeout', 'servicioPublicidad'];

    //Avisos de los comercios (pet shops, veterinarias, etc.) para la pantalla de inicio.
    //Uso: <wd-publicidad audience="Customers" latitude="cliente.latitude" longitude="cliente.longitude"></wd-publicidad>
    //audience: Customers (clientes) o Walkers (paseadores). latitude/longitude (opcionales): donde esta quien mira, para sumar los avisos por zona.
    //Si no hay avisos para mostrar, no se dibuja nada. Cada vez que se muestran se cuenta una vista y al tocarlos, un clic.
    function wdPublicidad($http, $window, $timeout, servicioPublicidad) {
        return {
            restrict: 'E',
            scope: { audience: '@', latitude: '=', longitude: '=' },
            templateUrl: '/scripts/spa/directives/publicidad.html',
            link: link
        };

        function link(scope) {
            scope.avisos = [];
            scope.titulo = scope.audience === 'Walkers' ? 'Comercios amigos' : 'Comercios cerca tuyo';
            scope.textoCategoria = servicioPublicidad.textoCategoria;

            //Los avisos cuya vista ya se conto (una sola vez por aviso mientras la pantalla esta abierta)
            var contados = {};
            var espera = null;

            function numero(valor) {
                var leido = parseFloat(valor);
                return isNaN(leido) ? null : leido;
            }

            function cargar() {
                var parametros = { audience: scope.audience === 'Walkers' ? 'Walkers' : 'Customers', max: 3 };
                var latitud = numero(scope.latitude);
                var longitud = numero(scope.longitude);
                if (latitud !== null && longitud !== null) {
                    parametros.latitude = latitud;
                    parametros.longitude = longitud;
                }

                $http.get('/api/ads/active', { params: parametros }).then(function (respuesta) {
                    scope.avisos = respuesta.data;

                    var nuevos = scope.avisos.map(function (aviso) { return aviso.id; }).filter(function (id) { return !contados[id]; });
                    if (nuevos.length > 0) {
                        angular.forEach(nuevos, function (id) { contados[id] = true; });
                        $http.post('/api/ads/impressions', { ids: nuevos });
                    }
                });
            }

            //Si cambia la ubicacion (por ejemplo, cuando termina de cargar el domicilio), se vuelven a buscar los avisos
            scope.$watchGroup(['latitude', 'longitude'], function () {
                $timeout.cancel(espera);
                espera = $timeout(cargar, 150);
            });
            scope.$on('$destroy', function () { $timeout.cancel(espera); });

            scope.abrir = function (aviso) {
                $http.post('/api/ads/click', { id: aviso.id });
                if (aviso.linkUrl) {
                    $window.open(aviso.linkUrl, '_blank', 'noopener');
                }
            };

            scope.textoDistancia = function (aviso) {
                if (aviso.distanceKm === null || aviso.distanceKm === undefined) {
                    return '';
                }
                return aviso.distanceKm < 1 ? 'a menos de 1 km' : 'a ' + String(aviso.distanceKm).replace('.', ',') + ' km';
            };
        }
    }

})(angular.module('common.core'));

(function (app) {
    'use strict';

    app.controller('comerciosAmigosCtrl', comerciosAmigosCtrl);

    comerciosAmigosCtrl.$inject = ['$scope', '$rootScope', '$http', '$window', 'servicioApi', 'servicioPublicidad'];

    //Directorio "Comercios amigos": veterinarias, guardias veterinarias, forrajerias, pet shops, guarderias, etc.,
    //agrupados por rubro, con los mas cercanos primero si se sabe donde esta quien mira
    function comerciosAmigosCtrl($scope, $rootScope, $http, $window, servicioApi, servicioPublicidad) {
        var usuario = $rootScope.repository.loggedUser;
        var esPaseador = usuario.roleId == '3';

        $scope.categorias = servicioPublicidad.categorias;
        $scope.comercios = [];
        $scope.grupos = [];
        $scope.cargado = false;
        $scope.conUbicacion = false;
        $scope.filtro = { categoria: '', texto: '' };

        //Para que el filtro por texto no distinga mayusculas ni acentos
        function normalizar(texto) {
            var minusculas = (texto || '').toLowerCase();
            return minusculas.normalize ? minusculas.normalize('NFD').replace(/[̀-ͯ]/g, '') : minusculas;
        }

        function coincideTexto(comercio) {
            var buscado = normalizar($scope.filtro.texto.trim());
            if (!buscado) {
                return true;
            }
            return normalizar([comercio.name, comercio.description, comercio.address].join(' ')).indexOf(buscado) !== -1;
        }

        //Agrupa por rubro (en el orden del servicio) los comercios que cumplen el filtro; los rubros sin comercios no se muestran
        $scope.actualizar = function () {
            var elegidos = $scope.comercios.filter(function (comercio) {
                return (!$scope.filtro.categoria || comercio.category === $scope.filtro.categoria) && coincideTexto(comercio);
            });

            $scope.grupos = $scope.categorias.map(function (categoria) {
                return {
                    codigo: categoria.codigo,
                    titulo: categoria.plural,
                    icono: categoria.icono,
                    comercios: elegidos.filter(function (comercio) { return comercio.category === categoria.codigo; })
                };
            }).filter(function (grupo) { return grupo.comercios.length > 0; });

            $scope.cantidadMostrada = elegidos.length;
        };

        //Cuantos comercios tiene un rubro (para los botones de filtro); null = todos
        $scope.cantidadDe = function (codigo) {
            return $scope.comercios.filter(function (comercio) { return !codigo || comercio.category === codigo; }).length;
        };

        $scope.elegirCategoria = function (codigo) {
            $scope.filtro.categoria = $scope.filtro.categoria === codigo ? '' : codigo;
            $scope.actualizar();
        };

        $scope.textoDistancia = function (comercio) {
            if (comercio.distanceKm === null || comercio.distanceKm === undefined) {
                return '';
            }
            return comercio.distanceKm < 1 ? 'a menos de 1 km' : 'a ' + String(comercio.distanceKm).replace('.', ',') + ' km';
        };

        $scope.enlaceComoLlegar = function (comercio) {
            return 'https://www.google.com/maps/dir/?api=1&destination=' + comercio.latitude + ',' + comercio.longitude;
        };

        $scope.abrirWeb = function (comercio) {
            $window.open(comercio.website, '_blank', 'noopener');
        };

        $scope.abrirPromocion = function (promocion) {
            $http.post('/api/ads/click', { id: promocion.adId });
            if (promocion.linkUrl) {
                $window.open(promocion.linkUrl, '_blank', 'noopener');
            }
        };

        function cargarComercios(latitud, longitud) {
            var parametros = { audience: esPaseador ? 'Walkers' : 'Customers' };
            if (latitud !== null && longitud !== null) {
                parametros.latitude = latitud;
                parametros.longitude = longitud;
            }

            $http.get('/api/ads/friends', { params: parametros }).then(function (respuesta) {
                $scope.comercios = respuesta.data;
                $scope.conUbicacion = latitud !== null && longitud !== null;
                $scope.cargado = true;
                $scope.actualizar();

                //Las promociones que se muestran cuentan como vistas (una vez por pantalla abierta)
                var ids = [];
                angular.forEach($scope.comercios, function (comercio) {
                    angular.forEach(comercio.promotions, function (promocion) { ids.push(promocion.adId); });
                });
                for (var i = 0; i < ids.length; i += 10) {
                    $http.post('/api/ads/impressions', { ids: ids.slice(i, i + 10) });
                }
            });
        }

        function numero(valor) {
            var leido = parseFloat(valor);
            return isNaN(leido) ? null : leido;
        }

        //La ubicacion de quien mira: el domicilio del cliente o la zona del paseador
        function iniciar() {
            var ruta = esPaseador ? '/api/walkers/getByUserId' : '/api/customers/getByUserId';
            servicioApi.get(ruta, { params: { userId: usuario.id } }, function (resultado) {
                cargarComercios(numero(resultado.data.latitude), numero(resultado.data.longitude));
            }, function () {
                cargarComercios(null, null);
            });
        }

        iniciar();
    }

})(angular.module('walkyDoggy'));

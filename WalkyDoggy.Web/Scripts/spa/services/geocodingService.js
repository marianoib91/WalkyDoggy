(function (app) {
    'use strict';

    app.factory('servicioGeocodificacion', servicioGeocodificacion);

    servicioGeocodificacion.$inject = ['$q'];

    //Busqueda de direcciones con Photon (datos de OpenStreetMap). Es gratuito y no necesita clave.
    //Se usa fetch a proposito: asi no viajan las cabeceras de $http (por ejemplo la de Authorization) a un servidor externo.
    //Para cambiar de proveedor solo hay que tocar este archivo.
    function servicioGeocodificacion($q) {
        var urlBase = 'https://photon.komoot.io';
        var limitesArgentina = '-73.6,-55.2,-53.5,-21.7';

        var servicio = {
            search: search,
            reverse: reverse
        };

        function get(path, parametros) {
            var query = Object.keys(parametros).filter(function (key) {
                return parametros[key] !== null && parametros[key] !== undefined;
            }).map(function (key) {
                return encodeURIComponent(key) + '=' + encodeURIComponent(parametros[key]);
            }).join('&');

            return $q.when(fetch(urlBase + path + '?' + query)).then(function (respuesta) {
                if (!respuesta.ok) {
                    return $q.reject(respuesta.status);
                }
                return respuesta.json();
            });
        }

        function aLugar(resultado) {
            var propiedades = resultado.properties || {};
            var coordinates = resultado.geometry.coordinates;

            return {
                street: propiedades.street || (propiedades.osm_key === 'highway' ? propiedades.name : null),
                houseNumber: propiedades.housenumber || null,
                city: propiedades.city || propiedades.locality || propiedades.town || propiedades.village || propiedades.district || propiedades.county || null,
                state: propiedades.state || null,
                postcode: propiedades.postcode || null,
                countryCode: propiedades.countrycode,
                latitude: coordinates[1],
                longitude: coordinates[0]
            };
        }

        //Devuelve hasta 6 direcciones de Argentina que coincidan con el texto. "bias" es un punto {lat, lng} para priorizar lo cercano.
        function search(texto, sesgo) {
            var parametros = {
                q: texto,
                limit: 10,
                bbox: limitesArgentina,
                lat: sesgo ? sesgo.lat : null,
                lon: sesgo ? sesgo.lng : null
            };

            return get('/api/', parametros).then(function (data) {
                var seen = {};
                return (data.features || []).map(aLugar).filter(function (lugar) {
                    if (lugar.countryCode !== 'AR' || !lugar.street) {
                        return false;
                    }
                    var key = [lugar.street, lugar.houseNumber, lugar.city, lugar.state].join('|');
                    if (seen[key]) {
                        return false;
                    }
                    seen[key] = true;
                    return true;
                }).slice(0, 6);
            });
        }

        //Direccion que corresponde a un punto del mapa (o null si no se reconoce)
        function reverse(latitude, longitude) {
            return get('/reverse', { lat: latitude, lon: longitude, limit: 1 }).then(function (data) {
                var resultado = data.features && data.features[0];
                if (!resultado) {
                    return null;
                }
                var lugar = aLugar(resultado);
                return lugar.countryCode === 'AR' ? lugar : null;
            });
        }

        return servicio;
    }

})(angular.module('common.core'));

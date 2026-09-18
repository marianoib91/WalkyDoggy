(function (app) {
    'use strict';

    app.factory('geocodingService', geocodingService);

    geocodingService.$inject = ['$q'];

    //Busqueda de direcciones con Photon (datos de OpenStreetMap). Es gratuito y no necesita clave.
    //Se usa fetch a proposito: asi no viajan las cabeceras de $http (por ejemplo la de Authorization) a un servidor externo.
    //Para cambiar de proveedor solo hay que tocar este archivo.
    function geocodingService($q) {
        var baseUrl = 'https://photon.komoot.io';
        var argentinaBoundingBox = '-73.6,-55.2,-53.5,-21.7';

        var service = {
            search: search,
            reverse: reverse
        };

        function get(path, params) {
            var query = Object.keys(params).filter(function (key) {
                return params[key] !== null && params[key] !== undefined;
            }).map(function (key) {
                return encodeURIComponent(key) + '=' + encodeURIComponent(params[key]);
            }).join('&');

            return $q.when(fetch(baseUrl + path + '?' + query)).then(function (response) {
                if (!response.ok) {
                    return $q.reject(response.status);
                }
                return response.json();
            });
        }

        function toPlace(feature) {
            var properties = feature.properties || {};
            var coordinates = feature.geometry.coordinates;

            return {
                street: properties.street || (properties.osm_key === 'highway' ? properties.name : null),
                houseNumber: properties.housenumber || null,
                city: properties.city || properties.locality || properties.town || properties.village || properties.district || properties.county || null,
                state: properties.state || null,
                postcode: properties.postcode || null,
                countryCode: properties.countrycode,
                latitude: coordinates[1],
                longitude: coordinates[0]
            };
        }

        //Devuelve hasta 6 direcciones de Argentina que coincidan con el texto. "bias" es un punto {lat, lng} para priorizar lo cercano.
        function search(text, bias) {
            var params = {
                q: text,
                limit: 10,
                bbox: argentinaBoundingBox,
                lat: bias ? bias.lat : null,
                lon: bias ? bias.lng : null
            };

            return get('/api/', params).then(function (data) {
                var seen = {};
                return (data.features || []).map(toPlace).filter(function (place) {
                    if (place.countryCode !== 'AR' || !place.street) {
                        return false;
                    }
                    var key = [place.street, place.houseNumber, place.city, place.state].join('|');
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
                var feature = data.features && data.features[0];
                if (!feature) {
                    return null;
                }
                var place = toPlace(feature);
                return place.countryCode === 'AR' ? place : null;
            });
        }

        return service;
    }

})(angular.module('common.core'));

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
        var nombreCaba = 'Ciudad Autónoma de Buenos Aires';

        var servicio = {
            search: search,
            reverse: reverse
        };

        //lang=default pide los nombres locales (en español): sin eso Photon usa el idioma del navegador y, por ejemplo,
        //devuelve "Autonomous City of Buenos Aires" cuando el navegador esta en ingles.
        function get(path, parametros) {
            parametros.lang = 'default';

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
            var esCaba = propiedades.state === nombreCaba || propiedades.state === 'Autonomous City of Buenos Aires';

            return {
                street: propiedades.street || (propiedades.osm_key === 'highway' ? propiedades.name : null),
                houseNumber: propiedades.housenumber || null,
                //En la Ciudad de Buenos Aires Photon informa el barrio (Coghlan, Palermo...) y no la ciudad: la ciudad es la misma para todos
                city: esCaba ? nombreCaba : (propiedades.city || propiedades.locality || propiedades.town || propiedades.village || propiedades.district || propiedades.county || null),
                barrio: esCaba ? (propiedades.district || null) : null,
                state: esCaba ? nombreCaba : (propiedades.state || null),
                postcode: propiedades.postcode || null,
                countryCode: propiedades.countrycode,
                nombre: propiedades.name || null,
                latitude: coordinates[1],
                longitude: coordinates[0]
            };
        }

        //Photon es estricto con el texto: "Capital Federal" no devuelve nada y "CABA" si. Se unifican los nombres mas usados de la Ciudad de Buenos Aires.
        function normalizarTexto(texto) {
            return texto.replace(/\bcapital\s+federal\b|\bc\.\s?a\.\s?b\.\s?a\.?/gi, 'CABA').replace(/\s+/g, ' ').trim();
        }

        //Si el texto completo no encuentra nada se prueba con menos datos: sin la ultima parte (provincia, pais...) y, al final, solo calle y numero
        function variantesDeBusqueda(texto) {
            var variantes = [normalizarTexto(texto)];
            var partes = variantes[0].split(',').map(function (parte) { return parte.trim(); }).filter(Boolean);

            for (var cantidad = partes.length - 1; cantidad >= 1; cantidad--) {
                variantes.push(partes.slice(0, cantidad).join(', '));
            }

            var calleYNumero = /^[^,\d]*\d+/.exec(variantes[0]);
            if (calleYNumero) {
                variantes.push(calleYNumero[0].trim());
            }

            return variantes.filter(function (variante, indice) {
                return variante && variantes.indexOf(variante) === indice;
            });
        }

        function buscarVariante(texto, sesgo) {
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

        //Devuelve hasta 6 direcciones de Argentina que coincidan con el texto. "sesgo" es un punto {lat, lng} para priorizar lo cercano.
        function search(texto, sesgo) {
            var variantes = variantesDeBusqueda(texto);

            function probar(indice) {
                return buscarVariante(variantes[indice], sesgo).then(function (lugares) {
                    if (lugares.length > 0 || indice === variantes.length - 1) {
                        return lugares;
                    }
                    return probar(indice + 1);
                });
            }

            return probar(0);
        }

        //Direccion que corresponde a un punto del mapa (o null si no se reconoce).
        //Si el punto cae sobre una plaza, un parque o un edificio sin calle, se usa la calle mas cercana; con aceptarLugar
        //(direcciones de referencia) se conserva el nombre del lugar, por ejemplo "Parque de la Independencia".
        function reverse(latitude, longitude, aceptarLugar) {
            return get('/reverse', { lat: latitude, lon: longitude, limit: 5 }).then(function (data) {
                var lugares = (data.features || []).map(aLugar).filter(function (lugar) {
                    return lugar.countryCode === 'AR';
                });
                if (lugares.length === 0) {
                    return null;
                }

                var masCercano = lugares[0];
                if (aceptarLugar && !masCercano.street && masCercano.nombre) {
                    return angular.extend({}, masCercano, { street: masCercano.nombre });
                }

                return lugares.filter(function (lugar) { return !!lugar.street; })[0] || null;
            });
        }

        return servicio;
    }

})(angular.module('common.core'));

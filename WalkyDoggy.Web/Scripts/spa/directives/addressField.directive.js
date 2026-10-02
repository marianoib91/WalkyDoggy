(function (app) {
    'use strict';

    app.directive('wdAddressField', wdAddressField);

    wdAddressField.$inject = ['$timeout', 'servicioGeocodificacion', 'servicioApi', 'servicioNotificaciones'];

    //Campo de domicilio con autocompletado y mapa.
    //Uso: <wd-address-field address="usuario"></wd-address-field>
    //Completa en "address": streetName, streetNumber, cityId, provinceId, cityName, provinceName, latitude y longitude.
    function wdAddressField($timeout, servicioGeocodificacion, servicioApi, servicioNotificaciones) {
        var contadorInstancias = 0;

        return {
            restrict: 'E',
            scope: { address: '=', allowLocate: '@' },
            templateUrl: '/scripts/spa/directives/addressField.html',
            link: link
        };

        function link(scope, element) {
            var centroPorDefecto = [-32.9468, -60.6393]; //Rosario
            var zoomPorDefecto = 12;
            var zoomPin = 17;
            var iconoPin = L.divIcon({
                className: 'wd-pin',
                html: '<svg width="34" height="44" viewBox="0 0 34 44" aria-hidden="true"><path d="M17 43C17 43 31 27.5 31 16.5 31 8.5 24.7 2 17 2S3 8.5 3 16.5C3 27.5 17 43 17 43z" fill="#166A4F" stroke="#ffffff" stroke-width="2.5"/><circle cx="17" cy="16.5" r="5.5" fill="#ffffff"/></svg>',
                iconSize: [34, 44],
                iconAnchor: [17, 43]
            });

            var mapa = null;
            var marcador = null;
            var temporizadorBusqueda = null;
            var tokenBusqueda = 0;
            var inicializado = false;

            scope.uid = 'wd-address-' + (++contadorInstancias);
            scope.state = { query: '', open: false, activeIndex: -1, searching: false, locating: false, message: null };
            scope.suggestions = [];

            crearMapa();

            scope.$watch('address', function (direccion) {
                if (!direccion || inicializado) {
                    return;
                }
                inicializado = true;
                mostrarDireccionExistente(direccion);
            });

            scope.$on('$destroy', function () {
                $timeout.cancel(temporizadorBusqueda);
                if (mapa) {
                    mapa.remove();
                }
            });

            /* ---------- Buscador ---------- */

            scope.onQueryChange = function () {
                $timeout.cancel(temporizadorBusqueda);
                scope.state.message = null;

                var texto = (scope.state.query || '').trim();
                if (texto.length < 3) {
                    scope.suggestions = [];
                    scope.state.open = false;
                    return;
                }

                temporizadorBusqueda = $timeout(function () {
                    ejecutarBusqueda(texto);
                }, 300);
            };

            scope.onKeydown = function (event) {
                if (!scope.state.open || scope.suggestions.length === 0) {
                    return;
                }

                var key = event.key || { 40: 'ArrowDown', 38: 'ArrowUp', 13: 'Enter', 27: 'Escape' }[event.keyCode];

                if (key === 'ArrowDown' || key === 'Down') {
                    scope.state.activeIndex = Math.min(scope.suggestions.length - 1, scope.state.activeIndex + 1);
                    event.preventDefault();
                } else if (key === 'ArrowUp' || key === 'Up') {
                    scope.state.activeIndex = Math.max(0, scope.state.activeIndex - 1);
                    event.preventDefault();
                } else if (key === 'Enter' && scope.state.activeIndex >= 0) {
                    event.preventDefault();
                    scope.select(scope.suggestions[scope.state.activeIndex]);
                } else if (key === 'Escape' || key === 'Esc') {
                    scope.state.open = false;
                }
            };

            scope.closeSoon = function () {
                $timeout(function () {
                    scope.state.open = false;
                }, 150);
            };

            scope.select = function (suggestion) {
                scope.state.open = false;
                scope.suggestions = [];
                aplicarLugar(suggestion.place);
            };

            //Usa la ubicacion del dispositivo (el navegador la pide con permiso; solo funciona en https o localhost)
            scope.locate = function () {
                if (!navigator.geolocation) {
                    scope.state.message = 'Tu navegador no permite obtener la ubicación. Buscá la dirección a mano.';
                    return;
                }

                scope.state.locating = true;
                scope.state.message = null;

                navigator.geolocation.getCurrentPosition(function (position) {
                    $timeout(function () {
                        var punto = { lat: position.coords.latitude, lng: position.coords.longitude };
                        scope.state.locating = false;
                        colocarPin(punto, true);
                        invertirYAplicar(punto, 'Usamos tu ubicación actual: revisá que la calle y el número sean los correctos.');
                    });
                }, function () {
                    $timeout(function () {
                        scope.state.locating = false;
                        scope.state.message = 'No pudimos obtener tu ubicación. Revisá el permiso del navegador o buscá la dirección a mano.';
                    });
                }, { enableHighAccuracy: true, timeout: 10000 });
            };

            function ejecutarBusqueda(texto) {
                var token = ++tokenBusqueda;
                scope.state.searching = true;

                servicioGeocodificacion.search(texto, puntoSesgo()).then(function (lugares) {
                    if (token !== tokenBusqueda) {
                        return;
                    }
                    scope.suggestions = lugares.map(function (lugar) {
                        return {
                            place: lugar,
                            title: lugar.street + (lugar.houseNumber ? ' ' + lugar.houseNumber : ''),
                            subtitle: [lugar.city, lugar.state].filter(Boolean).join(', ')
                        };
                    });
                    scope.state.activeIndex = scope.suggestions.length ? 0 : -1;
                    scope.state.open = scope.suggestions.length > 0;
                    scope.state.searching = false;
                    scope.state.message = scope.suggestions.length ? null : 'No encontramos esa dirección. Probá agregando la ciudad.';
                }, function () {
                    if (token !== tokenBusqueda) {
                        return;
                    }
                    scope.state.searching = false;
                    scope.state.message = 'No pudimos buscar direcciones en este momento. Podés completar la dirección a mano.';
                });
            }

            function puntoSesgo() {
                var punto = marcador ? marcador.getLatLng() : mapa.getCenter();
                return { lat: punto.lat, lng: punto.lng };
            }

            /* ---------- Datos de la direccion ---------- */

            function aplicarLugar(lugar) {
                var direccion = scope.address;
                var number = leerNumero(lugar.houseNumber);

                direccion.streetName = truncar(lugar.street, 50);
                direccion.streetNumber = number;
                scope.state.query = formatearEtiqueta(lugar.street, number, lugar.city, lugar.state);
                scope.state.message = number === null ? 'Esa calle no tiene el número cargado en el mapa: completalo a mano.' : null;

                colocarPin({ lat: lugar.latitude, lng: lugar.longitude }, true);
                resolverCiudad(lugar);

                if (number === null) {
                    $timeout(function () {
                        var entradaNumero = element[0].querySelector('.wd-address-number');
                        if (entradaNumero) {
                            entradaNumero.focus();
                        }
                    });
                }
            }

            //La ciudad se busca en la base (o se crea) para guardar el mismo desglose de siempre: ciudad -> provincia
            function resolverCiudad(lugar) {
                var direccion = scope.address;

                if (!lugar.city || !lugar.state) {
                    direccion.cityId = null;
                    direccion.provinceId = null;
                    direccion.cityName = null;
                    direccion.provinceName = null;
                    scope.state.message = 'No pudimos reconocer la ciudad de esa dirección. Probá con otra.';
                    return;
                }

                var criterios = { provinceName: lugar.state, cityName: lugar.city, postalCode: lugar.postcode };
                servicioApi.post('/api/cities/resolve', criterios, function (resultado) {
                    direccion.cityId = resultado.data.id;
                    direccion.provinceId = resultado.data.provinceId;
                    direccion.cityName = resultado.data.name;
                    direccion.provinceName = lugar.state;
                }, function () {
                    direccion.cityId = null;
                    direccion.provinceId = null;
                    direccion.cityName = null;
                    direccion.provinceName = null;
                    servicioNotificaciones.mostrarError('No se pudo reconocer la ciudad o la provincia de esa dirección.');
                });
            }

            function mostrarDireccionExistente(direccion) {
                var tieneCoordenadas = aLatLng(direccion.latitude, direccion.longitude);

                if (direccion.streetName) {
                    scope.state.query = formatearEtiqueta(direccion.streetName, direccion.streetNumber, direccion.cityName, direccion.provinceName);
                }

                if (tieneCoordenadas) {
                    colocarPin(tieneCoordenadas, false);
                    mapa.setView(tieneCoordenadas, zoomPin);
                } else if (direccion.streetName && direccion.cityName) {
                    //Domicilios cargados antes del mapa: se ubican por su texto para proponer el pin
                    var texto = [direccion.streetName, direccion.streetNumber, direccion.cityName, direccion.provinceName].filter(Boolean).join(' ');
                    servicioGeocodificacion.search(texto, null).then(function (lugares) {
                        if (lugares.length && !marcador) {
                            colocarPin({ lat: lugares[0].latitude, lng: lugares[0].longitude }, false);
                            mapa.setView(marcador.getLatLng(), zoomPin);
                        }
                    });
                }
            }

            /* ---------- Mapa ---------- */

            function crearMapa() {
                var container = element[0].querySelector('.wd-address-map');

                mapa = L.map(container, {
                    center: centroPorDefecto,
                    zoom: zoomPorDefecto,
                    scrollWheelZoom: false,
                    dragging: !L.Browser.mobile,
                    tap: !L.Browser.mobile
                });

                L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>'
                }).addTo(mapa);

                mapa.on('click', function (event) {
                    $timeout(function () {
                        colocarPin(event.latlng, false);
                        invertirYAplicar(event.latlng);
                    });
                });

                //El mapa se crea antes de que el formulario termine de acomodarse, por eso se recalcula el tamaño
                $timeout(function () {
                    mapa.invalidateSize();
                }, 300);
            }

            function colocarPin(latlng, centrarMapa) {
                var punto = L.latLng(latlng.lat, latlng.lng);

                if (!marcador) {
                    marcador = L.marker(punto, { draggable: true, icon: iconoPin, keyboard: false, title: 'Arrastrá el pin para ajustar la ubicación' }).addTo(mapa);
                    marcador.on('dragend', function () {
                        $timeout(function () {
                            var position = marcador.getLatLng();
                            guardarCoordenadas(position);
                            invertirYAplicar(position);
                        });
                    });
                } else {
                    marcador.setLatLng(punto);
                }

                guardarCoordenadas(punto);

                if (centrarMapa) {
                    mapa.setView(punto, zoomPin);
                }
            }

            function guardarCoordenadas(punto) {
                scope.address.latitude = punto.lat.toFixed(6);
                scope.address.longitude = punto.lng.toFixed(6);
            }

            //Al mover el pin se busca la calle de ese punto; si no hay numero se conserva el que ya estaba cargado
            function invertirYAplicar(punto, mensajeDeExito) {
                servicioGeocodificacion.reverse(punto.lat, punto.lng).then(function (lugar) {
                    if (!lugar || !lugar.street) {
                        scope.state.message = 'No pudimos identificar la calle en ese punto. Corregí la dirección a mano si hace falta.';
                        return;
                    }

                    var direccion = scope.address;
                    var number = leerNumero(lugar.houseNumber);
                    direccion.streetName = truncar(lugar.street, 50);
                    if (number !== null) {
                        direccion.streetNumber = number;
                    }
                    scope.state.query = formatearEtiqueta(direccion.streetName, direccion.streetNumber, lugar.city, lugar.state);
                    scope.state.message = mensajeDeExito || 'Ajustaste el pin: revisá que la calle y el número sean los correctos.';
                    resolverCiudad(lugar);
                });
            }

            /* ---------- Utilidades ---------- */

            function aLatLng(latitude, longitude) {
                var lat = parseFloat(latitude);
                var lng = parseFloat(longitude);
                return (isNaN(lat) || isNaN(lng)) ? null : { lat: lat, lng: lng };
            }

            function leerNumero(valor) {
                var match = /^\d+/.exec(valor || '');
                return match ? parseInt(match[0], 10) : null;
            }

            function truncar(texto, length) {
                return (texto || '').substring(0, length);
            }

            function formatearEtiqueta(calle, number, ciudad, provincia) {
                var linea = [calle, number].filter(Boolean).join(' ');
                return [linea, ciudad, provincia].filter(Boolean).join(', ');
            }
        }
    }

})(angular.module('common.core'));

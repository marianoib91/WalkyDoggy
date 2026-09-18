(function (app) {
    'use strict';

    app.directive('wdAddressField', wdAddressField);

    wdAddressField.$inject = ['$timeout', 'geocodingService', 'apiService', 'notificationService'];

    //Campo de domicilio con autocompletado y mapa.
    //Uso: <wd-address-field address="usuario"></wd-address-field>
    //Completa en "address": streetName, streetNumber, cityId, provinceId, cityName, provinceName, latitude y longitude.
    function wdAddressField($timeout, geocodingService, apiService, notificationService) {
        var instanceCounter = 0;

        return {
            restrict: 'E',
            scope: { address: '=', allowLocate: '@' },
            templateUrl: '/scripts/spa/directives/addressField.html',
            link: link
        };

        function link(scope, element) {
            var defaultCenter = [-32.9468, -60.6393]; //Rosario
            var defaultZoom = 12;
            var pinZoom = 17;
            var pinIcon = L.divIcon({
                className: 'wd-pin',
                html: '<svg width="34" height="44" viewBox="0 0 34 44" aria-hidden="true"><path d="M17 43C17 43 31 27.5 31 16.5 31 8.5 24.7 2 17 2S3 8.5 3 16.5C3 27.5 17 43 17 43z" fill="#166A4F" stroke="#ffffff" stroke-width="2.5"/><circle cx="17" cy="16.5" r="5.5" fill="#ffffff"/></svg>',
                iconSize: [34, 44],
                iconAnchor: [17, 43]
            });

            var map = null;
            var marker = null;
            var searchTimer = null;
            var searchToken = 0;
            var initialised = false;

            scope.uid = 'wd-address-' + (++instanceCounter);
            scope.state = { query: '', open: false, activeIndex: -1, searching: false, locating: false, message: null };
            scope.suggestions = [];

            createMap();

            scope.$watch('address', function (address) {
                if (!address || initialised) {
                    return;
                }
                initialised = true;
                showExistingAddress(address);
            });

            scope.$on('$destroy', function () {
                $timeout.cancel(searchTimer);
                if (map) {
                    map.remove();
                }
            });

            /* ---------- Buscador ---------- */

            scope.onQueryChange = function () {
                $timeout.cancel(searchTimer);
                scope.state.message = null;

                var text = (scope.state.query || '').trim();
                if (text.length < 3) {
                    scope.suggestions = [];
                    scope.state.open = false;
                    return;
                }

                searchTimer = $timeout(function () {
                    runSearch(text);
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
                applyPlace(suggestion.place);
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
                        var point = { lat: position.coords.latitude, lng: position.coords.longitude };
                        scope.state.locating = false;
                        placePin(point, true);
                        reverseAndApply(point, 'Usamos tu ubicación actual: revisá que la calle y el número sean los correctos.');
                    });
                }, function () {
                    $timeout(function () {
                        scope.state.locating = false;
                        scope.state.message = 'No pudimos obtener tu ubicación. Revisá el permiso del navegador o buscá la dirección a mano.';
                    });
                }, { enableHighAccuracy: true, timeout: 10000 });
            };

            function runSearch(text) {
                var token = ++searchToken;
                scope.state.searching = true;

                geocodingService.search(text, biasPoint()).then(function (places) {
                    if (token !== searchToken) {
                        return;
                    }
                    scope.suggestions = places.map(function (place) {
                        return {
                            place: place,
                            title: place.street + (place.houseNumber ? ' ' + place.houseNumber : ''),
                            subtitle: [place.city, place.state].filter(Boolean).join(', ')
                        };
                    });
                    scope.state.activeIndex = scope.suggestions.length ? 0 : -1;
                    scope.state.open = scope.suggestions.length > 0;
                    scope.state.searching = false;
                    scope.state.message = scope.suggestions.length ? null : 'No encontramos esa dirección. Probá agregando la ciudad.';
                }, function () {
                    if (token !== searchToken) {
                        return;
                    }
                    scope.state.searching = false;
                    scope.state.message = 'No pudimos buscar direcciones en este momento. Podés completar la dirección a mano.';
                });
            }

            function biasPoint() {
                var point = marker ? marker.getLatLng() : map.getCenter();
                return { lat: point.lat, lng: point.lng };
            }

            /* ---------- Datos de la direccion ---------- */

            function applyPlace(place) {
                var address = scope.address;
                var number = parseNumber(place.houseNumber);

                address.streetName = truncate(place.street, 50);
                address.streetNumber = number;
                scope.state.query = formatLabel(place.street, number, place.city, place.state);
                scope.state.message = number === null ? 'Esa calle no tiene el número cargado en el mapa: completalo a mano.' : null;

                placePin({ lat: place.latitude, lng: place.longitude }, true);
                resolveCity(place);

                if (number === null) {
                    $timeout(function () {
                        var numberInput = element[0].querySelector('.wd-address-number');
                        if (numberInput) {
                            numberInput.focus();
                        }
                    });
                }
            }

            //La ciudad se busca en la base (o se crea) para guardar el mismo desglose de siempre: ciudad -> provincia
            function resolveCity(place) {
                var address = scope.address;

                if (!place.city || !place.state) {
                    address.cityId = null;
                    address.provinceId = null;
                    address.cityName = null;
                    address.provinceName = null;
                    scope.state.message = 'No pudimos reconocer la ciudad de esa dirección. Probá con otra.';
                    return;
                }

                var criteria = { provinceName: place.state, cityName: place.city, postalCode: place.postcode };
                apiService.post('/api/cities/resolve', criteria, function (result) {
                    address.cityId = result.data.id;
                    address.provinceId = result.data.provinceId;
                    address.cityName = result.data.name;
                    address.provinceName = place.state;
                }, function () {
                    address.cityId = null;
                    address.provinceId = null;
                    address.cityName = null;
                    address.provinceName = null;
                    notificationService.displayError('No se pudo reconocer la ciudad o la provincia de esa dirección.');
                });
            }

            function showExistingAddress(address) {
                var hasCoordinates = toLatLng(address.latitude, address.longitude);

                if (address.streetName) {
                    scope.state.query = formatLabel(address.streetName, address.streetNumber, address.cityName, address.provinceName);
                }

                if (hasCoordinates) {
                    placePin(hasCoordinates, false);
                    map.setView(hasCoordinates, pinZoom);
                } else if (address.streetName && address.cityName) {
                    //Domicilios cargados antes del mapa: se ubican por su texto para proponer el pin
                    var text = [address.streetName, address.streetNumber, address.cityName, address.provinceName].filter(Boolean).join(' ');
                    geocodingService.search(text, null).then(function (places) {
                        if (places.length && !marker) {
                            placePin({ lat: places[0].latitude, lng: places[0].longitude }, false);
                            map.setView(marker.getLatLng(), pinZoom);
                        }
                    });
                }
            }

            /* ---------- Mapa ---------- */

            function createMap() {
                var container = element[0].querySelector('.wd-address-map');

                map = L.map(container, {
                    center: defaultCenter,
                    zoom: defaultZoom,
                    scrollWheelZoom: false,
                    dragging: !L.Browser.mobile,
                    tap: !L.Browser.mobile
                });

                L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>'
                }).addTo(map);

                map.on('click', function (event) {
                    $timeout(function () {
                        placePin(event.latlng, false);
                        reverseAndApply(event.latlng);
                    });
                });

                //El mapa se crea antes de que el formulario termine de acomodarse, por eso se recalcula el tamaño
                $timeout(function () {
                    map.invalidateSize();
                }, 300);
            }

            function placePin(latlng, centerMap) {
                var point = L.latLng(latlng.lat, latlng.lng);

                if (!marker) {
                    marker = L.marker(point, { draggable: true, icon: pinIcon, keyboard: false, title: 'Arrastrá el pin para ajustar la ubicación' }).addTo(map);
                    marker.on('dragend', function () {
                        $timeout(function () {
                            var position = marker.getLatLng();
                            saveCoordinates(position);
                            reverseAndApply(position);
                        });
                    });
                } else {
                    marker.setLatLng(point);
                }

                saveCoordinates(point);

                if (centerMap) {
                    map.setView(point, pinZoom);
                }
            }

            function saveCoordinates(point) {
                scope.address.latitude = point.lat.toFixed(6);
                scope.address.longitude = point.lng.toFixed(6);
            }

            //Al mover el pin se busca la calle de ese punto; si no hay numero se conserva el que ya estaba cargado
            function reverseAndApply(point, successMessage) {
                geocodingService.reverse(point.lat, point.lng).then(function (place) {
                    if (!place || !place.street) {
                        scope.state.message = 'No pudimos identificar la calle en ese punto. Corregí la dirección a mano si hace falta.';
                        return;
                    }

                    var address = scope.address;
                    var number = parseNumber(place.houseNumber);
                    address.streetName = truncate(place.street, 50);
                    if (number !== null) {
                        address.streetNumber = number;
                    }
                    scope.state.query = formatLabel(address.streetName, address.streetNumber, place.city, place.state);
                    scope.state.message = successMessage || 'Ajustaste el pin: revisá que la calle y el número sean los correctos.';
                    resolveCity(place);
                });
            }

            /* ---------- Utilidades ---------- */

            function toLatLng(latitude, longitude) {
                var lat = parseFloat(latitude);
                var lng = parseFloat(longitude);
                return (isNaN(lat) || isNaN(lng)) ? null : { lat: lat, lng: lng };
            }

            function parseNumber(value) {
                var match = /^\d+/.exec(value || '');
                return match ? parseInt(match[0], 10) : null;
            }

            function truncate(text, length) {
                return (text || '').substring(0, length);
            }

            function formatLabel(street, number, city, province) {
                var line = [street, number].filter(Boolean).join(' ');
                return [line, city, province].filter(Boolean).join(', ');
            }
        }
    }

})(angular.module('common.core'));

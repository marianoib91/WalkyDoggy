(function (app) {
    'use strict';

    app.controller('adminPublicidadCtrl', adminPublicidadCtrl);

    adminPublicidadCtrl.$inject = ['$scope', '$upload', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioPublicidad'];

    //Publicidad: los comercios que se anuncian (pet shops, veterinarias, transporte de mascotas, forrajerias...) y sus avisos
    function adminPublicidadCtrl($scope, $upload, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioPublicidad) {
        var tamanoMaximoImagen = 5 * 1024 * 1024;
        var textosEstado = { Running: 'Se muestra', Scheduled: 'Programado', Finished: 'Terminó', Paused: 'Pausado' };
        var textosAudiencia = { All: 'Clientes y paseadores', Customers: 'Solo clientes', Walkers: 'Solo paseadores' };

        //Alcances que se ofrecen (km alrededor del comercio); 0 = sin limite de zona
        $scope.alcances = [
            { valor: 0, texto: 'Todos, sin límite de zona' },
            { valor: 1, texto: 'Hasta 1 km del comercio' },
            { valor: 3, texto: 'Hasta 3 km del comercio' },
            { valor: 5, texto: 'Hasta 5 km del comercio' },
            { valor: 10, texto: 'Hasta 10 km del comercio' },
            { valor: 20, texto: 'Hasta 20 km del comercio' },
            { valor: 50, texto: 'Hasta 50 km del comercio' }
        ];
        $scope.audiencias = [
            { valor: 'All', texto: 'Clientes y paseadores' },
            { valor: 'Customers', texto: 'Solo clientes' },
            { valor: 'Walkers', texto: 'Solo paseadores' }
        ];
        $scope.categorias = servicioPublicidad.categorias;
        $scope.textoCategoria = servicioPublicidad.textoCategoria;

        //avisos | comercios
        $scope.seccion = 'avisos';
        $scope.comercios = [];
        $scope.avisos = [];
        $scope.guardando = false;

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        function formatoFecha(fecha) {
            return moment(fecha).format('YYYY-MM-DD');
        }

        function cargarComercios(alTerminar) {
            servicioApi.get('/api/admin/ads/advertisers', null, function (resultado) {
                $scope.comercios = resultado.data;
                if (alTerminar) { alTerminar(); }
            }, function (error) { mostrarError(error, 'No se pudieron cargar los comercios.'); });
        }

        function cargarAvisos() {
            servicioApi.get('/api/admin/ads', null, function (resultado) {
                $scope.avisos = resultado.data;
            }, function (error) { mostrarError(error, 'No se pudieron cargar los avisos.'); });
        }

        function cargarTodo() {
            cargarComercios();
            cargarAvisos();
        }

        $scope.cambiarSeccion = function (seccion) {
            $scope.seccion = seccion;
            cargarTodo();
        };

        /* ---------- Comercios ---------- */

        function comercioVacio() {
            return { id: 0, nombre: '', categoria: '', descripcion: '', telefono: '', web: '', direccion: {}, clave: Math.random() };
        }

        $scope.comercio = comercioVacio();
        $scope.errorComercio = '';

        $scope.comerciosActivos = function () {
            return $scope.comercios.filter(function (comercio) { return comercio.active; });
        };

        $scope.limpiarComercio = function () {
            $scope.comercio = comercioVacio();
            $scope.errorComercio = '';
        };

        $scope.editarComercio = function (comercio) {
            $scope.seccion = 'comercios';
            $scope.errorComercio = '';
            $scope.comercio = {
                id: comercio.id,
                nombre: comercio.name,
                categoria: comercio.category,
                descripcion: comercio.description || '',
                telefono: comercio.phone || '',
                web: comercio.website || '',
                direccion: comercio.latitude ? {
                    streetName: comercio.streetName,
                    streetNumber: comercio.streetNumber,
                    cityName: comercio.cityName,
                    latitude: comercio.latitude,
                    longitude: comercio.longitude
                } : { streetName: comercio.streetName, streetNumber: comercio.streetNumber, cityName: comercio.cityName },
                clave: Math.random()
            };
        };

        $scope.guardarComercio = function () {
            var c = $scope.comercio;
            var direccion = c.direccion || {};
            $scope.errorComercio = '';

            if (!(c.nombre || '').trim()) {
                $scope.errorComercio = 'Escribí el nombre del comercio.';
                return;
            }
            if (!c.categoria) {
                $scope.errorComercio = 'Elegí el rubro del comercio.';
                return;
            }

            $scope.guardando = true;
            servicioApi.post('/api/admin/ads/advertisers/save', {
                id: c.id,
                name: c.nombre,
                category: c.categoria,
                description: c.descripcion,
                phone: c.telefono,
                website: c.web,
                streetName: direccion.streetName || '',
                streetNumber: direccion.streetNumber ? Number(direccion.streetNumber) : null,
                cityName: direccion.cityName || '',
                latitude: direccion.latitude ? String(direccion.latitude) : '',
                longitude: direccion.longitude ? String(direccion.longitude) : ''
            }, function () {
                $scope.guardando = false;
                servicioNotificaciones.mostrarExito(c.id ? 'Guardaste los cambios del comercio.' : 'Agregaste el comercio.');
                $scope.limpiarComercio();
                cargarTodo();
            }, function (error) {
                $scope.guardando = false;
                $scope.errorComercio = error.data && error.data[0] ? error.data[0] : 'No se pudo guardar el comercio.';
            });
        };

        $scope.alternarComercio = function (comercio) {
            var activar = !comercio.active;
            servicioConfirmacion.preguntar(activar
                ? { title: '¿Reactivar a ' + comercio.name + '?', text: 'Sus avisos vuelven a mostrarse (los que estén vigentes).', confirmLabel: 'Reactivar', cancelLabel: 'Volver' }
                : { title: '¿Dar de baja a ' + comercio.name + '?', text: 'Sus avisos dejan de mostrarse, pero no se borran y sus estadísticas quedan. Podés reactivarlo cuando quieras.', confirmLabel: 'Dar de baja', cancelLabel: 'Volver', danger: true }
            ).then(function () {
                servicioApi.post('/api/admin/ads/advertisers/setActive', { id: comercio.id, active: activar }, function () {
                    servicioNotificaciones.mostrarExito(activar ? 'Reactivaste el comercio.' : 'Diste de baja el comercio.');
                    cargarTodo();
                }, function (error) { mostrarError(error, 'No se pudo cambiar el estado.'); });
            });
        };

        $scope.direccionDe = function (comercio) {
            var calle = [comercio.streetName, comercio.streetNumber].filter(Boolean).join(' ');
            return [calle, comercio.cityName].filter(Boolean).join(', ');
        };

        $scope.nuevoAvisoPara = function (comercio) {
            $scope.seccion = 'avisos';
            $scope.limpiarAviso();
            $scope.aviso.comercio = comercio.id;
        };

        /* ---------- Avisos ---------- */

        function avisoVacio() {
            return {
                id: 0, comercio: null, titulo: '', texto: '', enlace: '', desde: new Date(), hasta: moment().add(30, 'days').toDate(),
                audiencia: 'All', alcance: 0, imagenActual: null, imagenNueva: null, imagenVista: null, quitarImagen: false
            };
        }

        $scope.aviso = avisoVacio();
        $scope.errorAviso = '';

        function liberarVista() {
            if ($scope.aviso.imagenVista) {
                URL.revokeObjectURL($scope.aviso.imagenVista);
            }
        }

        $scope.limpiarAviso = function () {
            liberarVista();
            $scope.aviso = avisoVacio();
            $scope.errorAviso = '';
        };

        $scope.editarAviso = function (aviso) {
            liberarVista();
            $scope.seccion = 'avisos';
            $scope.errorAviso = '';
            $scope.aviso = {
                id: aviso.id,
                comercio: aviso.advertiserId,
                titulo: aviso.title,
                texto: aviso.text || '',
                enlace: aviso.linkUrl || '',
                desde: moment(aviso.startDate).toDate(),
                hasta: moment(aviso.endDate).toDate(),
                audiencia: aviso.audience,
                alcance: aviso.radiusKm || 0,
                imagenActual: aviso.imageUrl,
                imagenNueva: null,
                imagenVista: null,
                quitarImagen: false
            };
        };

        //El comercio elegido en el formulario (para avisar si no tiene ubicacion y para la vista previa)
        $scope.comercioElegido = function () {
            for (var i = 0; i < $scope.comercios.length; i++) {
                if ($scope.comercios[i].id === $scope.aviso.comercio) {
                    return $scope.comercios[i];
                }
            }
            return null;
        };

        $scope.sinUbicacion = function () {
            var comercio = $scope.comercioElegido();
            return comercio && !comercio.latitude;
        };

        $scope.alElegirImagen = function ($files) {
            $scope.errorAviso = '';
            var archivo = $files && $files[0];
            if (!archivo) {
                return;
            }
            if (!/^image\/(jpeg|png|gif|webp)$/.test(archivo.type)) {
                $scope.errorAviso = 'La imagen tiene que ser jpg, png, gif o webp.';
                return;
            }
            if (archivo.size > tamanoMaximoImagen) {
                $scope.errorAviso = 'La imagen supera los 5 MB.';
                return;
            }

            liberarVista();
            $scope.aviso.imagenNueva = archivo;
            $scope.aviso.imagenVista = URL.createObjectURL(archivo);
            $scope.aviso.quitarImagen = false;
        };

        $scope.sacarImagen = function () {
            liberarVista();
            $scope.aviso.imagenNueva = null;
            $scope.aviso.imagenVista = null;
            $scope.aviso.quitarImagen = !!$scope.aviso.imagenActual;
        };

        //Lo que se ve en la vista previa: la imagen nueva, o la actual si no se la quito
        $scope.imagenParaVistaPrevia = function () {
            return $scope.aviso.imagenVista || ($scope.aviso.quitarImagen ? null : $scope.aviso.imagenActual);
        };

        function terminarGuardado(id) {
            $scope.guardando = false;
            servicioNotificaciones.mostrarExito(id ? 'Guardaste el aviso.' : 'Creaste el aviso.');
            $scope.limpiarAviso();
            cargarTodo();
        }

        $scope.guardarAviso = function () {
            var a = $scope.aviso;
            $scope.errorAviso = '';

            if (!a.comercio) {
                $scope.errorAviso = 'Elegí el comercio del aviso.';
                return;
            }
            if (!(a.titulo || '').trim()) {
                $scope.errorAviso = 'Escribí el título del aviso.';
                return;
            }
            if (!a.desde || !a.hasta) {
                $scope.errorAviso = 'Elegí desde y hasta qué día se muestra.';
                return;
            }

            $scope.guardando = true;
            servicioApi.post('/api/admin/ads/save', {
                id: a.id,
                advertiserId: a.comercio,
                title: a.titulo,
                text: a.texto,
                linkUrl: a.enlace,
                startDate: formatoFecha(a.desde),
                endDate: formatoFecha(a.hasta),
                audience: a.audiencia,
                radiusKm: a.alcance || null
            }, function (resultado) {
                var idAviso = resultado.data.id;

                //Despues de guardar el aviso se sube (o se quita) la imagen
                if (a.imagenNueva) {
                    $upload.upload({ url: 'api/admin/ads/' + idAviso + '/image', method: 'POST', file: a.imagenNueva }).success(function () {
                        terminarGuardado(a.id);
                    }).error(function (datos) {
                        $scope.guardando = false;
                        servicioNotificaciones.mostrarError('Se guardó el aviso, pero la imagen no se pudo subir: ' + (datos && datos[0] ? datos[0] : 'intentá de nuevo.'));
                        cargarTodo();
                    });
                } else if (a.quitarImagen) {
                    servicioApi.post('/api/admin/ads/' + idAviso + '/image/remove', {}, function () { terminarGuardado(a.id); }, function () { terminarGuardado(a.id); });
                } else {
                    terminarGuardado(a.id);
                }
            }, function (error) {
                $scope.guardando = false;
                $scope.errorAviso = error.data && error.data[0] ? error.data[0] : 'No se pudo guardar el aviso.';
            });
        };

        $scope.alternarAviso = function (aviso) {
            var activar = !aviso.active;
            servicioApi.post('/api/admin/ads/setActive', { id: aviso.id, active: activar }, function () {
                servicioNotificaciones.mostrarExito(activar ? 'Reanudaste el aviso.' : 'Pausaste el aviso.');
                cargarAvisos();
            }, function (error) { mostrarError(error, 'No se pudo cambiar el estado.'); });
        };

        $scope.textoEstado = function (aviso) { return textosEstado[aviso.status] || aviso.status; };
        $scope.claseEstado = function (aviso) {
            return aviso.status === 'Running' ? 'wd-status-confirmed' : aviso.status === 'Paused' ? 'wd-status-cancelled' : 'wd-status-pending';
        };
        $scope.textoAudiencia = function (aviso) { return textosAudiencia[aviso.audience] || aviso.audience; };
        $scope.textoVigencia = function (aviso) {
            return moment(aviso.startDate).format('DD/MM/YYYY') + ' al ' + moment(aviso.endDate).format('DD/MM/YYYY');
        };

        $scope.$on('$destroy', liberarVista);

        cargarTodo();
    }

})(angular.module('walkyDoggy'));

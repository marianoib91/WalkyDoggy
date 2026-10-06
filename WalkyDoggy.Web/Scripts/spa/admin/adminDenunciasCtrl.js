(function (app) {
    'use strict';

    app.controller('adminDenunciasCtrl', adminDenunciasCtrl);

    adminDenunciasCtrl.$inject = ['$scope', '$http', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioDenuncias'];

    //Lista de denuncias y su detalle: imagenes adjuntas, chat de la reserva y resolucion
    function adminDenunciasCtrl($scope, $http, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioDenuncias) {
        var tamanoPagina = 15;

        $scope.filtro = { estado: 'Active' };
        $scope.denuncias = [];
        $scope.total = 0;
        $scope.pagina = 1;
        $scope.cargando = false;

        $scope.detalle = null;
        $scope.imagenes = [];
        $scope.mensajes = null;
        $scope.resolucion = { tipo: '', nota: '' };
        $scope.trabajando = false;

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        /* ---------- Lista ---------- */

        function cargar(pagina) {
            $scope.cargando = true;
            var parametros = { page: pagina, pageSize: tamanoPagina };
            if ($scope.filtro.estado) { parametros.status = $scope.filtro.estado; }

            servicioApi.get('/api/admin/complaints', { params: parametros }, function (resultado) {
                $scope.denuncias = resultado.data.complaints;
                $scope.total = resultado.data.total;
                $scope.pagina = resultado.data.page;
                $scope.cargando = false;
            }, function (error) {
                $scope.cargando = false;
                mostrarError(error, 'No se pudieron cargar las denuncias.');
            });
        }

        $scope.buscar = function () { cargar(1); };
        $scope.hayAnterior = function () { return $scope.pagina > 1; };
        $scope.haySiguiente = function () { return $scope.pagina * tamanoPagina < $scope.total; };
        $scope.irA = function (pagina) { cargar(pagina); };

        $scope.textoMotivo = servicioDenuncias.textoMotivo;

        var textosEstado = { Open: 'Nueva', InReview: 'En revisión', Resolved: 'Resuelta' };
        $scope.textoEstado = function (denuncia) { return textosEstado[denuncia.status] || denuncia.status; };
        $scope.claseEstado = function (denuncia) {
            return denuncia.status === 'Resolved' ? 'wd-status-confirmed' : 'wd-status-pending';
        };

        $scope.textoRol = function (rol) { return rol === 'Walker' ? 'Paseador' : 'Cliente'; };

        var textosResolucion = { NoAction: 'Sin acción', Warning: 'Advertencia', Block: 'Bloqueo de la cuenta' };
        $scope.textoResolucion = function (resolucion) { return textosResolucion[resolucion] || resolucion; };

        /* ---------- Detalle ---------- */

        function liberarImagenes() {
            angular.forEach($scope.imagenes, function (imagen) {
                if (imagen.url) { URL.revokeObjectURL(imagen.url); }
            });
            $scope.imagenes = [];
        }

        //Las imagenes no tienen un enlace publico: se piden con la sesion del administrador y se muestran desde la memoria del navegador
        function cargarImagenes(denuncia) {
            liberarImagenes();
            $scope.imagenes = denuncia.images.map(function (imagen) { return { id: imagen.id, nombre: imagen.originalName, url: null }; });

            angular.forEach($scope.imagenes, function (imagen) {
                $http.get('/api/admin/complaints/' + denuncia.id + '/images/' + imagen.id, { responseType: 'blob' }).then(function (respuesta) {
                    imagen.url = URL.createObjectURL(respuesta.data);
                }, function () {
                    imagen.error = true;
                });
            });
        }

        function abrirDetalle(id) {
            servicioApi.get('/api/admin/complaints/' + id, null, function (resultado) {
                $scope.detalle = resultado.data;
                $scope.mensajes = null;
                $scope.resolucion = { tipo: '', nota: '' };
                cargarImagenes(resultado.data);
            }, function (error) { mostrarError(error, 'No se pudo abrir la denuncia.'); });
        }

        $scope.abrir = function (denuncia) { abrirDetalle(denuncia.id); };

        $scope.volver = function () {
            liberarImagenes();
            $scope.detalle = null;
            $scope.mensajes = null;
            cargar($scope.pagina);
        };

        $scope.verImagen = function (imagen) {
            if (imagen.url) { window.open(imagen.url, '_blank'); }
        };

        $scope.verChat = function () {
            servicioConfirmacion.preguntar({
                title: '¿Leer el chat de la reserva?',
                text: 'Es una conversación privada entre el cliente y el paseador. Solo se lee para resolver esta denuncia y la lectura queda registrada en la bitácora.',
                confirmLabel: 'Leer el chat',
                cancelLabel: 'Volver'
            }).then(function () {
                servicioApi.get('/api/admin/complaints/' + $scope.detalle.id + '/chat', null, function (resultado) {
                    $scope.mensajes = resultado.data;
                }, function (error) { mostrarError(error, 'No se pudo leer el chat.'); });
            });
        };

        $scope.ponerEnRevision = function () {
            $scope.trabajando = true;
            servicioApi.post('/api/admin/complaints/status', { id: $scope.detalle.id, status: 'InReview' }, function () {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarExito('La denuncia quedó en revisión.');
                abrirDetalle($scope.detalle.id);
            }, function (error) {
                $scope.trabajando = false;
                mostrarError(error, 'No se pudo cambiar el estado.');
            });
        };

        $scope.puedeResolver = function () {
            return $scope.resolucion.tipo && ($scope.resolucion.nota || '').trim().length >= 5;
        };

        $scope.resolver = function () {
            var detalle = $scope.detalle;
            var tipo = $scope.resolucion.tipo;

            var avisos = {
                NoAction: 'Se cierra la denuncia sin ninguna medida. La persona denunciada no se entera.',
                Warning: 'Se cierra la denuncia y se le envía por mail una advertencia a ' + detalle.reportedName + ', con tu nota como motivo.',
                Block: 'Se bloquea la cuenta de ' + detalle.reportedName + ', se cancelan sus reservas que todavía no empezaron y se le avisa por mail con tu nota como motivo.'
            };

            servicioConfirmacion.preguntar({
                title: '¿Resolver la denuncia con "' + $scope.textoResolucion(tipo).toLowerCase() + '"?',
                text: avisos[tipo],
                confirmLabel: 'Resolver',
                cancelLabel: 'Volver',
                danger: tipo === 'Block'
            }).then(function () {
                $scope.trabajando = true;
                servicioApi.post('/api/admin/complaints/resolve', { id: detalle.id, resolution: tipo, note: $scope.resolucion.nota.trim() }, function () {
                    $scope.trabajando = false;
                    servicioNotificaciones.mostrarExito('Resolviste la denuncia.');
                    abrirDetalle(detalle.id);
                }, function (error) {
                    $scope.trabajando = false;
                    mostrarError(error, 'No se pudo resolver la denuncia.');
                });
            });
        };

        $scope.$on('$destroy', liberarImagenes);

        cargar(1);
    }

})(angular.module('walkyDoggy'));

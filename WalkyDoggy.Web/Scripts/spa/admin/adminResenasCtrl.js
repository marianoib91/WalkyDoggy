(function (app) {
    'use strict';

    app.controller('adminResenasCtrl', adminResenasCtrl);

    adminResenasCtrl.$inject = ['$scope', '$timeout', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion'];

    //Moderacion: ocultar (y volver a mostrar) valoraciones de paseadores y reseñas de mascotas. Ocultar no borra.
    function adminResenasCtrl($scope, $timeout, servicioApi, servicioNotificaciones, servicioConfirmacion) {
        var tamanoPagina = 15;

        //valoraciones (las escriben los clientes sobre los paseadores) | mascotas (las escriben los paseadores sobre las mascotas)
        $scope.tipo = 'valoraciones';
        $scope.filtros = { estado: '', maximoEstrellas: '', texto: '' };
        $scope.items = [];
        $scope.total = 0;
        $scope.pagina = 1;
        $scope.cargando = false;

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        function ruta() {
            return $scope.tipo === 'valoraciones' ? '/api/admin/moderation/ratings' : '/api/admin/moderation/petReviews';
        }

        function cargar(pagina) {
            $scope.cargando = true;
            var parametros = { page: pagina, pageSize: tamanoPagina };
            if ($scope.filtros.estado) { parametros.status = $scope.filtros.estado; }
            if ($scope.filtros.maximoEstrellas) { parametros.maxStars = $scope.filtros.maximoEstrellas; }
            if ($scope.filtros.texto) { parametros.search = $scope.filtros.texto; }

            servicioApi.get(ruta(), { params: parametros }, function (resultado) {
                $scope.items = $scope.tipo === 'valoraciones' ? resultado.data.ratings : resultado.data.reviews;
                $scope.total = resultado.data.total;
                $scope.pagina = resultado.data.page;
                $scope.cargando = false;
            }, function (error) {
                $scope.cargando = false;
                mostrarError(error, 'No se pudieron cargar las reseñas.');
            });
        }

        $scope.cambiarTipo = function (tipo) {
            $scope.tipo = tipo;
            cargar(1);
        };

        $scope.buscar = function () { cargar(1); };

        var espera = null;
        $scope.alEscribir = function () {
            $timeout.cancel(espera);
            espera = $timeout(function () { cargar(1); }, 350);
        };
        $scope.$on('$destroy', function () { $timeout.cancel(espera); });

        $scope.hayAnterior = function () { return $scope.pagina > 1; };
        $scope.haySiguiente = function () { return $scope.pagina * tamanoPagina < $scope.total; };
        $scope.irA = function (pagina) { cargar(pagina); };

        $scope.ocultar = function (item) {
            servicioConfirmacion.preguntar({
                title: '¿Ocultar esta reseña?',
                text: 'Deja de mostrarse y de contar en los promedios, pero no se borra: la podés volver a mostrar cuando quieras. El motivo queda en la bitácora.',
                input: { label: 'Motivo', placeholder: 'Por ejemplo: lenguaje ofensivo' },
                confirmLabel: 'Ocultar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function (motivo) {
                servicioApi.post(ruta() + '/hide', { id: item.id, reason: motivo }, function () {
                    servicioNotificaciones.mostrarExito('La reseña quedó oculta.');
                    cargar($scope.pagina);
                }, function (error) { mostrarError(error, 'No se pudo ocultar la reseña.'); });
            });
        };

        $scope.mostrar = function (item) {
            servicioApi.post(ruta() + '/show', { id: item.id }, function () {
                servicioNotificaciones.mostrarExito('La reseña vuelve a mostrarse.');
                cargar($scope.pagina);
            }, function (error) { mostrarError(error, 'No se pudo volver a mostrar la reseña.'); });
        };

        cargar(1);
    }

})(angular.module('walkyDoggy'));

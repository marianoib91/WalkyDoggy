(function (app) {
    'use strict';

    app.factory('servicioSubidaArchivos', servicioSubidaArchivos);

    servicioSubidaArchivos.$inject = ['$rootScope', '$http', '$timeout', '$upload', 'servicioNotificaciones'];

    function servicioSubidaArchivos($rootScope, $http, $timeout, $upload, servicioNotificaciones) {

        var servicio = {
            subirImagenDePerfil: subirImagenDePerfil
        }

        function subirImagenDePerfil($files, tipoEntidad, idEntidad, alTerminar) {
            var $file = $files[0];
            if (!$file) return;

            $upload.upload({
                url: 'api/images/' + tipoEntidad + '/' + idEntidad,
                method: 'POST',
                file: $file
            }).progress(function (evt) {
            }).success(function (data, status, headers, config) {
                servicioNotificaciones.mostrarExito('Imagen actualizada con éxito');
                alTerminar(data.profileImage);
            }).error(function (data, status, headers, config) {
                servicioNotificaciones.mostrarError(data || 'No se pudo subir la imagen. Intente nuevamente');
            });
        }

        return servicio;
    }

})(angular.module('common.core'));
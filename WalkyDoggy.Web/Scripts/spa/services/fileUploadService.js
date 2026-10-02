(function (app) {
    'use strict';

    app.factory('servicioSubidaArchivos', servicioSubidaArchivos);

    servicioSubidaArchivos.$inject = ['$rootScope', '$http', '$timeout', '$upload', 'servicioNotificaciones'];

    function servicioSubidaArchivos($rootScope, $http, $timeout, $upload, servicioNotificaciones) {

        $rootScope.upload = [];

        var servicio = {
            uploadImage: uploadImage,
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

        function uploadImage($files, movieId, alTerminar) {
            //$files: lista de archivos seleccionados
            for (var i = 0; i < $files.length; i++) {
                var $file = $files[i];
                (function (indice) {
                    $rootScope.upload[indice] = $upload.upload({
                        url: "api/movies/images/upload?movieId=" + movieId, // url de la API
                        method: "POST",
                        file: $file
                    }).progress(function (evt) {
                    }).success(function (data, status, headers, config) {
                        // el archivo se subio correctamente
                        servicioNotificaciones.mostrarExito(data.FileName + ' uploaded successfully');
                        alTerminar();
                    }).error(function (data, status, headers, config) {
                        servicioNotificaciones.mostrarError(data.Message);
                    });
                })(i);
            }
        }

        return servicio;
    }

})(angular.module('common.core'));
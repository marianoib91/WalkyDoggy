(function (app) {
    'use strict';

    app.factory('fileUploadService', fileUploadService);

    fileUploadService.$inject = ['$rootScope', '$http', '$timeout', '$upload', 'notificationService'];

    function fileUploadService($rootScope, $http, $timeout, $upload, notificationService) {

        $rootScope.upload = [];

        var service = {
            uploadImage: uploadImage,
            uploadProfileImage: uploadProfileImage
        }

        function uploadProfileImage($files, entityType, entityId, callback) {
            var $file = $files[0];
            if (!$file) return;

            $upload.upload({
                url: 'api/images/' + entityType + '/' + entityId,
                method: 'POST',
                file: $file
            }).progress(function (evt) {
            }).success(function (data, status, headers, config) {
                notificationService.displaySuccess('Imagen actualizada con éxito');
                callback(data.profileImage);
            }).error(function (data, status, headers, config) {
                notificationService.displayError(data || 'No se pudo subir la imagen. Intente nuevamente');
            });
        }

        function uploadImage($files, movieId, callback) {
            //$files: an array of files selected
            for (var i = 0; i < $files.length; i++) {
                var $file = $files[i];
                (function (index) {
                    $rootScope.upload[index] = $upload.upload({
                        url: "api/movies/images/upload?movieId=" + movieId, // webapi url
                        method: "POST",
                        file: $file
                    }).progress(function (evt) {
                    }).success(function (data, status, headers, config) {
                        // file is uploaded successfully
                        notificationService.displaySuccess(data.FileName + ' uploaded successfully');
                        callback();
                    }).error(function (data, status, headers, config) {
                        notificationService.displayError(data.Message);
                    });
                })(i);
            }
        }

        return service;
    }

})(angular.module('common.core'));
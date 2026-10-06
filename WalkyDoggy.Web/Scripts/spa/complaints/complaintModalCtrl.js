(function (app) {
    'use strict';

    app.controller('denunciaModalCtrl', denunciaModalCtrl);

    denunciaModalCtrl.$inject = ['$scope', '$modalInstance', '$upload', 'servicioNotificaciones', 'servicioDenuncias', 'datos'];

    function denunciaModalCtrl($scope, $modalInstance, $upload, servicioNotificaciones, servicioDenuncias, datos) {
        var maximoImagenes = 5;
        var tamanoMaximo = 5 * 1024 * 1024;

        $scope.datos = datos;
        $scope.motivos = servicioDenuncias.motivos;
        $scope.formulario = { motivo: '', descripcion: '' };
        $scope.imagenes = [];
        $scope.error = '';
        $scope.enviando = false;

        $scope.maximoImagenes = maximoImagenes;

        //Se pueden ir sumando imagenes de a una o de a varias; cada una se controla antes de aceptarla
        $scope.alElegirImagenes = function ($files) {
            $scope.error = '';

            angular.forEach($files, function (archivo) {
                if (!/^image\/(jpeg|png|gif|webp)$/.test(archivo.type)) {
                    $scope.error = '"' + archivo.name + '" no es una imagen válida (jpg, png, gif o webp).';
                } else if (archivo.size > tamanoMaximo) {
                    $scope.error = '"' + archivo.name + '" supera los 5 MB.';
                } else if ($scope.imagenes.length >= maximoImagenes) {
                    $scope.error = 'Podés adjuntar hasta ' + maximoImagenes + ' imágenes.';
                } else {
                    $scope.imagenes.push({ archivo: archivo, vista: URL.createObjectURL(archivo) });
                }
            });
        };

        $scope.quitarImagen = function (indice) {
            URL.revokeObjectURL($scope.imagenes[indice].vista);
            $scope.imagenes.splice(indice, 1);
            $scope.error = '';
        };

        function liberarVistas() {
            angular.forEach($scope.imagenes, function (imagen) { URL.revokeObjectURL(imagen.vista); });
        }

        $scope.cancelar = function () {
            liberarVistas();
            $modalInstance.dismiss('cancel');
        };

        $scope.enviar = function () {
            var descripcion = ($scope.formulario.descripcion || '').trim();

            if (!$scope.formulario.motivo) {
                $scope.error = 'Elegí el motivo de la denuncia.';
                return;
            }
            if (descripcion.length < 10) {
                $scope.error = 'Contanos qué pasó (al menos 10 caracteres).';
                return;
            }

            $scope.error = '';
            $scope.enviando = true;

            var envio = {
                url: 'api/complaints/create',
                method: 'POST',
                data: {
                    bookingKey: datos.bookingKey,
                    actor: datos.actor,
                    actorId: String(datos.actorId),
                    reason: $scope.formulario.motivo,
                    description: descripcion
                }
            };
            if ($scope.imagenes.length > 0) {
                envio.file = $scope.imagenes.map(function (imagen) { return imagen.archivo; });
                envio.fileFormDataName = 'files';
            }

            $upload.upload(envio).success(function () {
                liberarVistas();
                servicioNotificaciones.mostrarExito('Enviamos tu denuncia. Un administrador la va a revisar.');
                $modalInstance.close();
            }).error(function (data) {
                $scope.enviando = false;
                $scope.error = data && data[0] ? data[0] : 'No se pudo enviar la denuncia. Intentá de nuevo.';
            });
        };
    }

})(angular.module('common.core'));

(function (app) {
    'use strict';

    app.factory('servicioNotificaciones', servicioNotificaciones);

    function servicioNotificaciones() {

        toastr.options = {
            "debug": false,
            "positionClass": "toast-top-right",
            "onclick": null,
            "fadeIn": 300,
            "fadeOut": 1000,
            "timeOut": 3000,
            "extendedTimeOut": 1000
        };

        var servicio = {
            mostrarExito: mostrarExito,
            mostrarError: mostrarError,
            mostrarAdvertencia: mostrarAdvertencia,
            mostrarInfo: mostrarInfo
        };

        return servicio;

        function mostrarExito(mensaje) {
            toastr.success(mensaje);
        }

        function mostrarError(error) {
            if (Array.isArray(error)) {
                error.forEach(function (err) {
                    toastr.error(err);
                });
            } else {
                toastr.error(error);
            }
        }

        function mostrarAdvertencia(mensaje) {
            toastr.warning(mensaje);
        }

        function mostrarInfo(mensaje) {
            toastr.info(mensaje);
        }

    }

})(angular.module('common.core'));
(function (app) {
    'use strict';

    app.factory('servicioSugerencia', servicioSugerencia);

    servicioSugerencia.$inject = ['servicioApi'];

    //La sugerencia de reserva del cliente (dia y horario que suele reservar, calculados con su historial). La usan Mis mascotas y la busqueda de paseadores.
    function servicioSugerencia(servicioApi) {
        var DIAS = ['domingos', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábados'];

        //Se pide la sugerencia; si el cliente no tiene un patron claro (204) o ya dijo "Ahora no" para esa fecha, no se llama a alCargar
        function cargar(idCliente, alCargar) {
            servicioApi.get('/api/predictions/nextBooking', { params: { customerId: idCliente } }, function (resultado) {
                if (resultado.status === 200 && resultado.data && !descartada(idCliente, resultado.data)) {
                    alCargar(resultado.data);
                }
            });
        }

        function clave(idCliente, sugerencia) {
            return 'wd-sugerencia-' + idCliente + '-' + sugerencia.nextDate;
        }

        function descartada(idCliente, sugerencia) {
            try {
                return !!window.localStorage.getItem(clave(idCliente, sugerencia));
            } catch (e) {
                return false;
            }
        }

        //"Ahora no": no se vuelve a mostrar para esa fecha
        function descartar(idCliente, sugerencia) {
            try {
                window.localStorage.setItem(clave(idCliente, sugerencia), '1');
            } catch (e) { }
        }

        //"Rex y Luna" / "Rex, Luna y Lola"
        function unir(nombres) {
            return nombres.length > 1 ? nombres.slice(0, -1).join(', ') + ' y ' + nombres[nombres.length - 1] : nombres[0];
        }

        function texto(sugerencia) {
            var verbo = sugerencia.petNames.length > 1 ? 'suelen' : 'suele';
            return unir(sugerencia.petNames) + ' ' + verbo + ' salir los ' + DIAS[sugerencia.dayOfWeek] + ' a las ' + sugerencia.time + '.';
        }

        function evidencia(sugerencia) {
            return 'Se ve en tus últimas ' + sugerencia.total + ' reservas: ' + sugerencia.matches + ' fueron los ' + DIAS[sugerencia.dayOfWeek] + '.';
        }

        return { cargar: cargar, descartar: descartar, texto: texto, evidencia: evidencia };
    }

})(angular.module('common.core'));

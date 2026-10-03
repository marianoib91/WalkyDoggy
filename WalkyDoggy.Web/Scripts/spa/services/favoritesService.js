(function (app) {
    'use strict';

    app.factory('servicioFavoritos', servicioFavoritos);

    servicioFavoritos.$inject = ['servicioApi', 'servicioNotificaciones'];

    //Paseadores favoritos del cliente. Se comparte entre la busqueda y el perfil del paseador: se cargan una vez y se mantienen al dia al marcar o desmarcar.
    function servicioFavoritos(servicioApi, servicioNotificaciones) {
        var ids = [];
        var cargadoPara = null;

        //alTerminar se llama con la lista de ids (si ya estaba cargada para ese cliente, no se vuelve a pedir)
        function cargar(idCliente, alTerminar) {
            if (cargadoPara === idCliente) {
                if (alTerminar) { alTerminar(ids); }
                return;
            }

            servicioApi.get('/api/favorites/list', { params: { customerId: idCliente } }, function (resultado) {
                ids = resultado.data;
                cargadoPara = idCliente;
                if (alTerminar) { alTerminar(ids); }
            });
        }

        function esFavorito(idPaseador) {
            return ids.indexOf(idPaseador) !== -1;
        }

        //Marca o desmarca. Se refleja enseguida y, si el servidor lo rechaza, se vuelve atras.
        function alternar(idCliente, idPaseador, alTerminar) {
            var marcar = !esFavorito(idPaseador);

            cambiarLocal(idPaseador, marcar);
            servicioApi.post('/api/favorites/set', { customerId: idCliente, walkerId: idPaseador, favorite: marcar }, function () {
                if (alTerminar) { alTerminar(marcar); }
            }, function (error) {
                cambiarLocal(idPaseador, !marcar);
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo cambiar el favorito. Intentá nuevamente.');
                if (alTerminar) { alTerminar(!marcar); }
            });
        }

        function cambiarLocal(idPaseador, marcar) {
            var posicion = ids.indexOf(idPaseador);
            if (marcar && posicion === -1) {
                ids.push(idPaseador);
            } else if (!marcar && posicion !== -1) {
                ids.splice(posicion, 1);
            }
        }

        return { cargar: cargar, esFavorito: esFavorito, alternar: alternar };
    }

})(angular.module('common.core'));

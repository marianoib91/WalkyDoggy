(function (app) {
    'use strict';

    app.factory('servicioChat', servicioChat);

    servicioChat.$inject = ['$modal'];

    //Abre el chat de una reserva (paseador y cliente). Devuelve una promesa que se resuelve cuando se cierra la ventana,
    //para que quien lo abrio actualice sus listas (por ejemplo, los mensajes sin leer).
    //Uso: servicioChat.abrir({ bookingKey: reserva.bookingKey, rol: 'Customer', actorId: idCliente, titulo: 'Chat con Pedro', subtitulo: 'Rex · 14:00' }).finally(...)
    function servicioChat($modal) {
        function abrir(datos) {
            return $modal.open({
                templateUrl: 'scripts/spa/chat/chatModal.html',
                controller: 'chatModalCtrl',
                windowClass: 'wd-chat-modal',
                resolve: { datos: function () { return datos; } }
            }).result;
        }

        return { abrir: abrir };
    }

})(angular.module('common.core'));

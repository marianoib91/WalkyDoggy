(function (app) {
    'use strict';

    app.factory('servicioDenuncias', servicioDenuncias);

    servicioDenuncias.$inject = ['$modal'];

    //Denuncias: abre el formulario para denunciar a la otra parte de una reserva (cliente o paseador) y guarda los textos de los motivos.
    //Uso: servicioDenuncias.abrir({ bookingKey: reserva.bookingKey, actor: 'Customer', actorId: idCliente, titulo: 'Denunciar a Pedro', subtitulo: 'Rex · lunes 05/10, 10:00' })
    //Devuelve una promesa que se resuelve cuando la denuncia se envio y se rechaza si se cerro la ventana sin enviar.
    function servicioDenuncias($modal) {
        var motivos = [
            { codigo: 'AnimalAbuse', texto: 'Maltrato a la mascota' },
            { codigo: 'DisrespectfulTreatment', texto: 'Trato irrespetuoso' },
            { codigo: 'NoShow', texto: 'No se presentó' },
            { codigo: 'UndueCharge', texto: 'Cobro indebido' },
            { codigo: 'Other', texto: 'Otro motivo' }
        ];

        function textoMotivo(codigo) {
            for (var i = 0; i < motivos.length; i++) {
                if (motivos[i].codigo === codigo) {
                    return motivos[i].texto;
                }
            }
            return codigo;
        }

        function abrir(datos) {
            return $modal.open({
                templateUrl: 'scripts/spa/complaints/complaintModal.html',
                controller: 'denunciaModalCtrl',
                resolve: { datos: function () { return datos; } }
            }).result;
        }

        return { abrir: abrir, motivos: motivos, textoMotivo: textoMotivo };
    }

})(angular.module('common.core'));

(function (app) {
    'use strict';

    app.factory('servicioPublicidad', servicioPublicidad);

    //Rubros de los comercios que se anuncian (los mismos codigos que valida el servidor), con su texto en singular y plural
    //y el icono con que se muestran en el directorio "Comercios amigos". El orden es el orden en que se listan.
    function servicioPublicidad() {
        var categorias = [
            { codigo: 'Veterinary', texto: 'Veterinaria', plural: 'Veterinarias', icono: 'fa-medkit' },
            { codigo: 'EmergencyVet', texto: 'Guardia veterinaria', plural: 'Guardias veterinarias', icono: 'fa-ambulance' },
            { codigo: 'PetShop', texto: 'Pet shop', plural: 'Pet shops', icono: 'fa-shopping-cart' },
            { codigo: 'FeedStore', texto: 'Forrajería', plural: 'Forrajerías', icono: 'fa-leaf' },
            { codigo: 'DayCare', texto: 'Guardería animal', plural: 'Guarderías de animales', icono: 'fa-home' },
            { codigo: 'Groomer', texto: 'Peluquería canina', plural: 'Peluquerías caninas', icono: 'fa-scissors' },
            { codigo: 'Trainer', texto: 'Adiestramiento', plural: 'Adiestramiento', icono: 'fa-graduation-cap' },
            { codigo: 'PetTransport', texto: 'Transporte de mascotas', plural: 'Transporte de mascotas', icono: 'fa-car' },
            { codigo: 'Other', texto: 'Otro rubro', plural: 'Otros comercios', icono: 'fa-map-marker' }
        ];

        function buscar(codigo) {
            for (var i = 0; i < categorias.length; i++) {
                if (categorias[i].codigo === codigo) {
                    return categorias[i];
                }
            }
            return null;
        }

        function textoCategoria(codigo) {
            var categoria = buscar(codigo);
            return categoria ? categoria.texto : codigo;
        }

        return { categorias: categorias, textoCategoria: textoCategoria };
    }

})(angular.module('common.core'));

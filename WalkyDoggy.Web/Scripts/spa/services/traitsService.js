(function (app) {
    'use strict';

    app.factory('servicioCaracteristicas', servicioCaracteristicas);

    servicioCaracteristicas.$inject = ['$http'];

    //Caracteristicas con las que se describe a un perro. Vienen en pares opuestos y de cada par se elige una sola (o ninguna).
    //El catalogo (los pares y sus textos) lo administra el administrador y se lee del servidor (api/traits/getAll, solo las activas);
    //se carga una vez al abrir la pagina y, hasta que llega, la lista de pares esta vacia.
    //Se guardan en la mascota como codigos separados por coma ("Playful,Runner"); el codigo no cambia aunque se renombre el texto.
    function servicioCaracteristicas($http) {
        //Es siempre el mismo arreglo (se llena cuando llega la respuesta), asi las pantallas que ya lo tomaron se actualizan solas
        var pares = [];
        var textos = {};
        var cargado = false;

        //Cuantas caracteristicas tienen que coincidir para considerar parecidos a dos perros (igual que en el servidor)
        var minimoEnComun = 2;

        $http.get('/api/traits/getAll').then(function (respuesta) {
            pares.length = 0;
            textos = {};

            angular.forEach(respuesta.data, function (par) {
                pares.push([
                    { codigo: par.first.code, texto: par.first.label },
                    { codigo: par.second.code, texto: par.second.label }
                ]);
                textos[par.first.code] = par.first.label;
                textos[par.second.code] = par.second.label;
            });
            cargado = true;
        });

        function textoDe(codigo) {
            return textos[codigo] || codigo;
        }

        //De "Playful,Runner" a ['Playful', 'Runner'] (sin las que el administrador dio de baja, una vez cargado el catalogo)
        function leer(guardadas) {
            var codigos = guardadas ? guardadas.split(',').filter(Boolean) : [];
            return cargado ? codigos.filter(function (codigo) { return textos.hasOwnProperty(codigo); }) : codigos;
        }

        //De ['Playful', 'Runner'] a "Juguetón, Corredor"
        function textosDe(codigos) {
            return (codigos || []).map(textoDe).join(', ');
        }

        return { pares: pares, minimoEnComun: minimoEnComun, textoDe: textoDe, leer: leer, textos: textosDe };
    }

})(angular.module('common.core'));

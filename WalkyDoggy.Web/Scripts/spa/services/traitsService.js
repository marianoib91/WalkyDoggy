(function (app) {
    'use strict';

    app.factory('servicioCaracteristicas', servicioCaracteristicas);

    //Caracteristicas con las que se describe a un perro. Vienen en pares opuestos y de cada par se elige una sola (o ninguna).
    //Los codigos son los mismos que valida el servidor (PetTraits); aca solo se les pone el texto en castellano.
    //Se guardan en la mascota como codigos separados por coma ("Playful,Runner").
    function servicioCaracteristicas() {
        var pares = [
            [{ codigo: 'Playful', texto: 'Juguetón' }, { codigo: 'Calm', texto: 'Tranquilo' }],
            [{ codigo: 'Extroverted', texto: 'Extrovertido' }, { codigo: 'Introverted', texto: 'Introvertido' }],
            [{ codigo: 'Runner', texto: 'Corredor' }, { codigo: 'SunNapper', texto: 'Siestas al sol' }],
            [{ codigo: 'Obedient', texto: 'Obediente' }, { codigo: 'Naughty', texto: 'Travieso' }],
            [{ codigo: 'Barker', texto: 'Ladrador' }, { codigo: 'Quiet', texto: 'Silencioso' }],
            [{ codigo: 'WaterLover', texto: 'Ama el agua' }, { codigo: 'WaterAverse', texto: 'Evita el agua' }],
            [{ codigo: 'Affectionate', texto: 'Cariñoso' }, { codigo: 'Independent', texto: 'Independiente' }]
        ];

        //Cuantas caracteristicas tienen que coincidir para considerar parecidos a dos perros (igual que en el servidor)
        var minimoEnComun = 2;

        function textoDe(codigo) {
            for (var i = 0; i < pares.length; i++) {
                for (var j = 0; j < pares[i].length; j++) {
                    if (pares[i][j].codigo === codigo) {
                        return pares[i][j].texto;
                    }
                }
            }
            return codigo;
        }

        //De "Playful,Runner" a ['Playful', 'Runner']
        function leer(guardadas) {
            return guardadas ? guardadas.split(',').filter(Boolean) : [];
        }

        //De ['Playful', 'Runner'] a "Juguetón, Corredor"
        function textos(codigos) {
            return (codigos || []).map(textoDe).join(', ');
        }

        return { pares: pares, minimoEnComun: minimoEnComun, textoDe: textoDe, leer: leer, textos: textos };
    }

})(angular.module('common.core'));

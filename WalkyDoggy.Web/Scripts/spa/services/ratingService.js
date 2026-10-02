(function (app) {
    'use strict';

    app.factory('servicioValoraciones', servicioValoraciones);

    servicioValoraciones.$inject = ['$modal'];

    //Dialogo para valorar un paseo: de 1 a 5 estrellas y un comentario opcional.
    //Devuelve una promesa que se resuelve con { stars, comment } si el cliente envia la valoracion y se rechaza si cancela.
    //Uso: servicioValoraciones.preguntar({ walkerName: 'Pedro Ramirez', petNames: 'Rex' }).then(function (rating) { ... })
    function servicioValoraciones($modal) {
        var etiquetas = ['Muy malo', 'Malo', 'Regular', 'Bueno', 'Excelente'];

        var plantilla =
            '<div class="wd-dialog wd-rating-dialog">' +
            '<h2 class="wd-dialog-title">Valorá a {{opciones.walkerName}}</h2>' +
            '<p class="wd-dialog-text">¿Cómo fue el paseo de {{opciones.petNames}}? Tu opinión ayuda a otros clientes a elegir.</p>' +
            '<div class="wd-rating-picker" role="radiogroup" aria-label="Estrellas" ng-mouseleave="hover = 0">' +
            '<button type="button" class="wd-rating-star" ng-repeat="star in [1, 2, 3, 4, 5]" role="radio" aria-checked="{{datos.stars === star}}"' +
            ' aria-label="{{star}} {{star === 1 ? \'estrella\' : \'estrellas\'}}" ng-mouseenter="hover = star" ng-click="datos.stars = star">' +
            '<i class="fa" ng-class="(hover || datos.stars) >= star ? \'fa-star\' : \'fa-star-o\'" aria-hidden="true"></i>' +
            '</button>' +
            '<span class="wd-rating-label">{{etiquetaDe(hover || datos.stars)}}</span>' +
            '</div>' +
            '<div class="form-group">' +
            '<label for="wd-rating-comment">Comentario (opcional)</label>' +
            '<textarea id="wd-rating-comment" class="form-control" rows="4" maxlength="500" ng-model="datos.comment" placeholder="Contá cómo fue la experiencia con el paseador."></textarea>' +
            '<p class="wd-field-hint">{{(datos.comment || \'\').length}} / 500</p>' +
            '</div>' +
            '<div class="wd-dialog-actions">' +
            '<button type="button" class="btn btn-default" ng-click="cancelar()">Volver</button>' +
            '<button type="button" class="btn btn-primary" ng-disabled="!datos.stars" ng-click="enviar()">Enviar valoración</button>' +
            '</div>' +
            '</div>';

        function preguntar(opciones) {
            return $modal.open({
                template: plantilla,
                size: 'sm',
                controller: ['$scope', '$modalInstance', function ($scope, $modalInstance) {
                    $scope.opciones = opciones || {};
                    $scope.datos = { stars: 0, comment: '' };
                    $scope.hover = 0;
                    $scope.etiquetaDe = function (stars) { return stars ? etiquetas[stars - 1] : 'Elegí una cantidad de estrellas'; };
                    $scope.enviar = function () { $modalInstance.close({ stars: $scope.datos.stars, comment: ($scope.datos.comment || '').trim() }); };
                    $scope.cancelar = function () { $modalInstance.dismiss('cancel'); };
                }]
            }).result;
        }

        return { preguntar: preguntar };
    }

})(angular.module('common.core'));

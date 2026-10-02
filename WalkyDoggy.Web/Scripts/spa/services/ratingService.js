(function (app) {
    'use strict';

    app.factory('ratingService', ratingService);

    ratingService.$inject = ['$modal'];

    //Dialogo para valorar un paseo: de 1 a 5 estrellas y un comentario opcional.
    //Devuelve una promesa que se resuelve con { stars, comment } si el cliente envia la valoracion y se rechaza si cancela.
    //Uso: ratingService.ask({ walkerName: 'Pedro Ramirez', petNames: 'Rex' }).then(function (rating) { ... })
    function ratingService($modal) {
        var labels = ['Muy malo', 'Malo', 'Regular', 'Bueno', 'Excelente'];

        var template =
            '<div class="wd-dialog wd-rating-dialog">' +
            '<h2 class="wd-dialog-title">Valorá a {{options.walkerName}}</h2>' +
            '<p class="wd-dialog-text">¿Cómo fue el paseo de {{options.petNames}}? Tu opinión ayuda a otros clientes a elegir.</p>' +
            '<div class="wd-rating-picker" role="radiogroup" aria-label="Estrellas" ng-mouseleave="hover = 0">' +
            '<button type="button" class="wd-rating-star" ng-repeat="star in [1, 2, 3, 4, 5]" role="radio" aria-checked="{{data.stars === star}}"' +
            ' aria-label="{{star}} {{star === 1 ? \'estrella\' : \'estrellas\'}}" ng-mouseenter="hover = star" ng-click="data.stars = star">' +
            '<i class="fa" ng-class="(hover || data.stars) >= star ? \'fa-star\' : \'fa-star-o\'" aria-hidden="true"></i>' +
            '</button>' +
            '<span class="wd-rating-label">{{labelOf(hover || data.stars)}}</span>' +
            '</div>' +
            '<div class="form-group">' +
            '<label for="wd-rating-comment">Comentario (opcional)</label>' +
            '<textarea id="wd-rating-comment" class="form-control" rows="4" maxlength="500" ng-model="data.comment" placeholder="Contá cómo fue la experiencia con el paseador."></textarea>' +
            '<p class="wd-field-hint">{{(data.comment || \'\').length}} / 500</p>' +
            '</div>' +
            '<div class="wd-dialog-actions">' +
            '<button type="button" class="btn btn-default" ng-click="cancel()">Volver</button>' +
            '<button type="button" class="btn btn-primary" ng-disabled="!data.stars" ng-click="send()">Enviar valoración</button>' +
            '</div>' +
            '</div>';

        function ask(options) {
            return $modal.open({
                template: template,
                size: 'sm',
                controller: ['$scope', '$modalInstance', function ($scope, $modalInstance) {
                    $scope.options = options || {};
                    $scope.data = { stars: 0, comment: '' };
                    $scope.hover = 0;
                    $scope.labelOf = function (stars) { return stars ? labels[stars - 1] : 'Elegí una cantidad de estrellas'; };
                    $scope.send = function () { $modalInstance.close({ stars: $scope.data.stars, comment: ($scope.data.comment || '').trim() }); };
                    $scope.cancel = function () { $modalInstance.dismiss('cancel'); };
                }]
            }).result;
        }

        return { ask: ask };
    }

})(angular.module('common.core'));

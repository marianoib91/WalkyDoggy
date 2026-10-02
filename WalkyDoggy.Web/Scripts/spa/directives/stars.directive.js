(function (app) {
    'use strict';

    app.directive('wdStars', wdStars);

    //Estrellas de solo lectura: <wd-stars value="walker.averageRating" size="lg"></wd-stars>
    //Admite decimales (4,5 muestra cuatro estrellas y media). Size: sm (por defecto), md o lg.
    function wdStars() {
        return {
            restrict: 'E',
            scope: { value: '=', size: '@' },
            template:
                '<span class="wd-stars" ng-class="size ? \'wd-stars-\' + size : \'\'" role="img" aria-label="{{label()}}">' +
                '<i ng-repeat="icon in icons() track by $index" class="fa" ng-class="icon" aria-hidden="true"></i>' +
                '</span>',
            link: function (scope) {
                scope.icons = function () {
                    var value = Number(scope.value) || 0;
                    var icons = [];
                    for (var star = 1; star <= 5; star++) {
                        icons.push(value >= star - 0.25 ? 'fa-star' : (value >= star - 0.75 ? 'fa-star-half-o' : 'fa-star-o'));
                    }
                    return icons;
                };

                scope.label = function () {
                    return 'Valoración de ' + (Number(scope.value) || 0) + ' sobre 5';
                };
            }
        };
    }

})(angular.module('common.ui'));

(function (app) {
    'use strict';

    app.factory('confirmService', confirmService);

    confirmService.$inject = ['$modal'];

    //Dialogo de confirmacion. Devuelve una promesa que se resuelve si el usuario acepta y se rechaza si cancela.
    //Uso: confirmService.ask({ title: '...', text: '...', confirmLabel: 'Cancelar paseo', cancelLabel: 'Volver', danger: true }).then(...)
    //Con input: { label: '...', placeholder: '...' } el dialogo pide un texto (obligatorio) y la promesa se resuelve con ese texto.
    function confirmService($modal) {
        var template =
            '<div class="wd-dialog">' +
            '<h2 class="wd-dialog-title">{{options.title}}</h2>' +
            '<p class="wd-dialog-text">{{options.text}}</p>' +
            '<div class="form-group" ng-if="options.input">' +
            '<label for="wd-dialog-input">{{options.input.label}}</label>' +
            '<textarea id="wd-dialog-input" class="form-control" rows="3" maxlength="500" placeholder="{{options.input.placeholder}}" ng-model="data.value"></textarea>' +
            '</div>' +
            '<div class="wd-dialog-actions">' +
            '<button type="button" class="btn btn-default" ng-click="cancel()">{{options.cancelLabel}}</button>' +
            '<button type="button" class="btn" ng-class="options.danger ? \'btn-danger\' : \'btn-primary\'" ng-disabled="options.input && !(data.value && data.value.trim())" ng-click="confirm()">{{options.confirmLabel}}</button>' +
            '</div>' +
            '</div>';

        function ask(options) {
            var settings = angular.extend({ confirmLabel: 'Aceptar', cancelLabel: 'Volver', danger: false }, options);

            return $modal.open({
                template: template,
                size: 'sm',
                controller: ['$scope', '$modalInstance', function ($scope, $modalInstance) {
                    $scope.options = settings;
                    $scope.data = { value: '' };
                    $scope.confirm = function () { $modalInstance.close(settings.input ? $scope.data.value.trim() : true); };
                    $scope.cancel = function () { $modalInstance.dismiss('cancel'); };
                }]
            }).result;
        }

        return { ask: ask };
    }

})(angular.module('common.core'));

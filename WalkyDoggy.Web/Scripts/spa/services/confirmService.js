(function (app) {
    'use strict';

    app.factory('servicioConfirmacion', servicioConfirmacion);

    servicioConfirmacion.$inject = ['$modal'];

    //Dialogo de confirmacion. Devuelve una promesa que se resuelve si el usuario acepta y se rechaza si cancela.
    //Uso: servicioConfirmacion.preguntar({ title: '...', text: '...', confirmLabel: 'Cancelar paseo', cancelLabel: 'Volver', danger: true }).then(...)
    //Con input: { label: '...', placeholder: '...' } el dialogo pide un texto (obligatorio) y la promesa se resuelve con ese texto.
    function servicioConfirmacion($modal) {
        var plantilla =
            '<div class="wd-dialog">' +
            '<h2 class="wd-dialog-title">{{opciones.title}}</h2>' +
            '<p class="wd-dialog-text">{{opciones.text}}</p>' +
            '<div class="form-group" ng-if="opciones.input">' +
            '<label for="wd-dialog-input">{{opciones.input.label}}</label>' +
            '<textarea id="wd-dialog-input" class="form-control" rows="3" maxlength="500" placeholder="{{opciones.input.placeholder}}" ng-model="datos.value"></textarea>' +
            '</div>' +
            '<div class="wd-dialog-actions">' +
            '<button type="button" class="btn btn-default" ng-click="cancelar()">{{opciones.cancelLabel}}</button>' +
            '<button type="button" class="btn" ng-class="opciones.danger ? \'btn-danger\' : \'btn-primary\'" ng-disabled="opciones.input && !(datos.value && datos.value.trim())" ng-click="confirmar()">{{opciones.confirmLabel}}</button>' +
            '</div>' +
            '</div>';

        function preguntar(opciones) {
            var configuracion = angular.extend({ confirmLabel: 'Aceptar', cancelLabel: 'Volver', danger: false }, opciones);

            return $modal.open({
                template: plantilla,
                size: 'sm',
                controller: ['$scope', '$modalInstance', function ($scope, $modalInstance) {
                    $scope.opciones = configuracion;
                    $scope.datos = { value: '' };
                    $scope.confirmar = function () { $modalInstance.close(configuracion.input ? $scope.datos.value.trim() : true); };
                    $scope.cancelar = function () { $modalInstance.dismiss('cancel'); };
                }]
            }).result;
        }

        return { preguntar: preguntar };
    }

})(angular.module('common.core'));

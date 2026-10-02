(function (app) {
    'use strict';

    app.filter('wdMoney', wdMoney);

    //Montos en pesos al estilo argentino: 4321.5 -> "4.321,50"; 3800 -> "3.800"
    //Uso: ${{walker.amount | wdMoney}}
    function wdMoney() {
        return function (value) {
            if (value === null || value === undefined || value === '') {
                return '';
            }

            var amount = Number(value);
            return amount.toLocaleString('es-AR', { minimumFractionDigits: amount % 1 ? 2 : 0, maximumFractionDigits: 2 });
        };
    }

})(angular.module('common.ui'));

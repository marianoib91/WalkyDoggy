(function (app) {
    'use strict';

    app.controller('condicionesLaboralesCtrl', condicionesLaboralesCtrl);

    condicionesLaboralesCtrl.$inject = ['$scope', 'servicioNotificaciones', 'servicioApi', '$rootScope', '$window'];

    function condicionesLaboralesCtrl($scope, servicioNotificaciones, servicioApi, $rootScope, $window) {
        var idPaseador = $rootScope.repository.loggedUser.walkerId;
        var pintando = null;

        $scope.dias = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
        $scope.horas = [];
        for (var hora = 0; hora < 24; hora++) {
            $scope.horas.push(hora);
        }

        //week[dia][hora] = true si trabaja en esa hora (la hora N va de N:00 a N+1:00)
        $scope.semana = {};
        angular.forEach($scope.dias, function (dia) { $scope.semana[dia] = diaVacio(); });

        $scope.paseador = null;
        $scope.form = { amount: null, maxPets: 3, description: '' };
        $scope.cantidadesDePerros = [1, 2, 3, 4, 5];
        $scope.guardando = false;
        $scope.cargado = false;

        iniciar();

        function diaVacio() {
            var celdas = [];
            for (var hora = 0; hora < 24; hora++) { celdas.push(false); }
            return celdas;
        }

        function formatearHora(hora) {
            return (hora < 10 ? '0' : '') + hora + ':00';
        }

        function iniciar() {
            servicioApi.get('/api/workDays/getAllByWalkerId', { params: { walkerId: idPaseador } }, function (resultado) {
                angular.forEach(resultado.data, function (jornada) {
                    var desde = Number(jornada.timeFrom.split(':')[0]);
                    var hasta = Number(jornada.timeUntil.split(':')[0]);
                    if ($scope.semana[jornada.dayOfWeek]) {
                        for (var hora = desde; hora < hasta && hora < 24; hora++) {
                            $scope.semana[jornada.dayOfWeek][hora] = true;
                        }
                    }
                });
            });

            servicioApi.get('/api/walkers/getByUserId', { params: { userId: $rootScope.repository.loggedUser.id } }, function (resultado) {
                $scope.paseador = resultado.data;
                $scope.form.amount = resultado.data.amount;
                $scope.form.maxPets = resultado.data.maxPetsAtOnce;
                $scope.form.description = resultado.data.description;
                $scope.cargado = true;
            });
        }

        /* ---------- Grilla semanal: se pinta arrastrando con el mouse o tocando cada hora ---------- */

        $scope.empezarAPintar = function (dia, hora, $event) {
            if ($event) {
                $event.preventDefault();
            }
            pintando = { value: !$scope.semana[dia][hora] };
            $scope.semana[dia][hora] = pintando.value;
        };

        $scope.pintarAlPasar = function (dia, hora) {
            if (pintando) {
                $scope.semana[dia][hora] = pintando.value;
            }
        };

        function dejarDePintar() {
            pintando = null;
        }
        $window.addEventListener('mouseup', dejarDePintar);
        $scope.$on('$destroy', function () { $window.removeEventListener('mouseup', dejarDePintar); });

        $scope.llenarDia = function (dia, valor) {
            for (var hora = 0; hora < 24; hora++) { $scope.semana[dia][hora] = valor; }
        };

        $scope.llenarTodo = function (valor) {
            angular.forEach($scope.dias, function (dia) { $scope.llenarDia(dia, valor); });
        };

        $scope.copiarATodos = function (origen) {
            angular.forEach($scope.dias, function (dia) {
                for (var hora = 0; hora < 24; hora++) { $scope.semana[dia][hora] = $scope.semana[origen][hora]; }
            });
            servicioNotificaciones.mostrarExito('Copiaste el horario del ' + origen.toLowerCase() + ' a todos los días.');
        };

        //Franjas continuas de un dia: [{ from: 8, until: 14 }, ...] (until = 24 es la medianoche)
        function franjasDe(dia) {
            var franjas = [];
            var hora = 0;
            while (hora < 24) {
                if (!$scope.semana[dia][hora]) { hora++; continue; }
                var inicio = hora;
                while (hora < 24 && $scope.semana[dia][hora]) { hora++; }
                franjas.push({ from: inicio, until: hora });
            }
            return franjas;
        }

        $scope.resumenDe = function (dia) {
            var franjas = franjasDe(dia);
            if (franjas.length === 0) {
                return 'No trabajás';
            }
            if (franjas.length === 1 && franjas[0].from === 0 && franjas[0].until === 24) {
                return 'Todo el día';
            }
            return franjas.map(function (range) { return formatearHora(range.from) + ' a ' + formatearHora(range.until); }).join(' · ');
        };

        $scope.horasSemanales = function () {
            var total = 0;
            angular.forEach($scope.dias, function (dia) {
                for (var hora = 0; hora < 24; hora++) { if ($scope.semana[dia][hora]) { total++; } }
            });
            return total;
        };

        $scope.diasTrabajados = function () {
            return $scope.dias.filter(function (dia) { return franjasDe(dia).length > 0; }).length;
        };

        /* ---------- Guardar ---------- */

        $scope.tarifaValida = function () {
            var monto = Number($scope.form.amount);
            return $scope.form.amount !== null && $scope.form.amount !== '' && monto >= 1 && monto <= 1000000;
        };

        $scope.enviar = function () {
            if ($scope.guardando) {
                return;
            }
            if (!$scope.tarifaValida()) {
                servicioNotificaciones.mostrarError('Ingresá una tarifa por hora válida (entre $1 y $1.000.000).');
                return;
            }
            if (!$scope.form.description || !$scope.form.description.trim()) {
                servicioNotificaciones.mostrarError('Contales a tus clientes sobre vos: la descripción es obligatoria.');
                return;
            }

            var franjas = [];
            angular.forEach($scope.dias, function (dia) {
                angular.forEach(franjasDe(dia), function (range) {
                    franjas.push({ dayOfWeek: dia, timeFrom: formatearHora(range.from), timeUntil: formatearHora(range.until) });
                });
            });

            $scope.guardando = true;
            servicioApi.post('/api/workDays/saveWeek', { walkerId: idPaseador, ranges: franjas }, function () {
                $scope.paseador.amount = Number($scope.form.amount);
                $scope.paseador.maxPetsAtOnce = $scope.form.maxPets;
                $scope.paseador.description = $scope.form.description.trim();

                servicioApi.post('/api/walkers/update', $scope.paseador, function () {
                    $scope.guardando = false;
                    servicioNotificaciones.mostrarExito('Condiciones laborales guardadas.');
                }, alFallarGuardado);
            }, alFallarGuardado);
        };

        function alFallarGuardado(error) {
            $scope.guardando = false;
            var mensaje = error && error.data && error.data[0] ? error.data[0] : 'No se pudieron guardar las condiciones laborales. Intentá nuevamente.';
            servicioNotificaciones.mostrarError(mensaje);
        }
    }

})(angular.module('common.core'));

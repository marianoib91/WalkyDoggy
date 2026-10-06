(function (app) {
    'use strict';

    app.controller('adminCatalogosCtrl', adminCatalogosCtrl);

    adminCatalogosCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion'];

    //Razas, tamaños y caracteristicas de las mascotas. $scope.pestana viene de la pantalla de administracion (razas | tamanos | caracteristicas).
    function adminCatalogosCtrl($scope, servicioApi, servicioNotificaciones, servicioConfirmacion) {
        var textos = {
            razas: { ruta: 'breeds', singular: 'raza', plural: 'razas', articulo: 'la', nueva: 'Nueva raza' },
            tamanos: { ruta: 'sizes', singular: 'tamaño', plural: 'tamaños', articulo: 'el', nueva: 'Nuevo tamaño' }
        };

        $scope.items = [];
        $scope.pares = [];
        $scope.nuevo = { nombre: '' };
        $scope.formulario = { pairId: 0, primera: '', segunda: '' };
        $scope.errorFormulario = '';
        $scope.guardando = false;

        function esCaracteristicas() {
            return $scope.pestana === 'caracteristicas';
        }

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        $scope.texto = function () {
            return textos[$scope.pestana] || textos.razas;
        };

        function cargar() {
            $scope.items = [];
            $scope.pares = [];
            $scope.nuevo.nombre = '';
            $scope.limpiarFormulario();

            if (esCaracteristicas()) {
                servicioApi.get('/api/admin/catalogs/traits', null, function (resultado) {
                    $scope.pares = resultado.data;
                }, function (error) { mostrarError(error, 'No se pudieron cargar las características.'); });
            } else {
                servicioApi.get('/api/admin/catalogs/' + $scope.texto().ruta, null, function (resultado) {
                    $scope.items = resultado.data;
                }, function (error) { mostrarError(error, 'No se pudo cargar la lista.'); });
            }
        }

        $scope.$watch('pestana', cargar);

        function avisarYRecargar(mensaje) {
            return function () {
                servicioNotificaciones.mostrarExito(mensaje);
                cargar();
            };
        }

        /* ---------- Razas y tamaños ---------- */

        $scope.agregar = function () {
            var nombre = ($scope.nuevo.nombre || '').trim();
            if (!nombre) {
                return;
            }

            $scope.guardando = true;
            servicioApi.post('/api/admin/catalogs/' + $scope.texto().ruta + '/save', { id: 0, name: nombre }, function () {
                $scope.guardando = false;
                avisarYRecargar('Agregaste ' + $scope.texto().articulo + ' ' + $scope.texto().singular + ' "' + nombre + '".')();
            }, function (error) {
                $scope.guardando = false;
                mostrarError(error, 'No se pudo agregar.');
            });
        };

        $scope.renombrar = function (item) {
            servicioConfirmacion.preguntar({
                title: 'Renombrar ' + $scope.texto().singular,
                text: 'Las mascotas que ya lo tienen cargado pasan a mostrar el nombre nuevo.',
                input: { label: 'Nombre', placeholder: item.name, value: item.name },
                confirmLabel: 'Guardar',
                cancelLabel: 'Volver'
            }).then(function (nombre) {
                servicioApi.post('/api/admin/catalogs/' + $scope.texto().ruta + '/save', { id: item.id, name: nombre },
                    avisarYRecargar('Cambiaste el nombre.'),
                    function (error) { mostrarError(error, 'No se pudo cambiar el nombre.'); });
            });
        };

        $scope.alternarActivo = function (item) {
            var activar = !item.active;
            var pregunta = activar
                ? { title: '¿Reactivar "' + item.name + '"?', text: 'Vuelve a ofrecerse al cargar mascotas.', confirmLabel: 'Reactivar' }
                : {
                    title: '¿Dar de baja "' + item.name + '"?',
                    text: 'Ya no se va a ofrecer al cargar mascotas nuevas. ' + (item.petCount > 0
                        ? 'Las ' + item.petCount + ' mascotas que ya lo tienen cargado lo conservan.'
                        : 'Ninguna mascota lo tiene cargado.') + ' Podés reactivarlo cuando quieras.',
                    confirmLabel: 'Dar de baja',
                    danger: true
                };
            pregunta.cancelLabel = 'Volver';

            servicioConfirmacion.preguntar(pregunta).then(function () {
                servicioApi.post('/api/admin/catalogs/' + $scope.texto().ruta + '/setActive', { id: item.id, active: activar },
                    avisarYRecargar(activar ? 'Reactivaste "' + item.name + '".' : 'Diste de baja "' + item.name + '".'),
                    function (error) { mostrarError(error, 'No se pudo cambiar el estado.'); });
            });
        };

        /* ---------- Caracteristicas ---------- */

        $scope.limpiarFormulario = function () {
            $scope.formulario = { pairId: 0, primera: '', segunda: '' };
            $scope.errorFormulario = '';
        };

        $scope.editarPar = function (par) {
            $scope.formulario = { pairId: par.pairId, primera: par.first.label, segunda: par.second.label };
            $scope.errorFormulario = '';
        };

        $scope.guardarPar = function () {
            var formulario = $scope.formulario;
            var primera = (formulario.primera || '').trim();
            var segunda = (formulario.segunda || '').trim();

            if (primera.length < 2 || segunda.length < 2) {
                $scope.errorFormulario = 'Escribí las dos características del par.';
                return;
            }

            $scope.errorFormulario = '';
            $scope.guardando = true;
            servicioApi.post('/api/admin/catalogs/traits/save', { pairId: formulario.pairId, firstLabel: primera, secondLabel: segunda }, function () {
                $scope.guardando = false;
                avisarYRecargar(formulario.pairId ? 'Guardaste los cambios.' : 'Agregaste el par.')();
            }, function (error) {
                $scope.guardando = false;
                $scope.errorFormulario = error.data && error.data[0] ? error.data[0] : 'No se pudo guardar el par.';
            });
        };

        $scope.alternarPar = function (par) {
            var activar = !par.active;
            var nombre = par.first.label + ' / ' + par.second.label;
            var pregunta = activar
                ? { title: '¿Reactivar "' + nombre + '"?', text: 'Vuelve a ofrecerse al cargar mascotas y a contar para sugerir paseadores.', confirmLabel: 'Reactivar' }
                : {
                    title: '¿Dar de baja "' + nombre + '"?',
                    text: 'Ya no se va a ofrecer ni va a contar para sugerir paseadores. ' + (par.petCount > 0
                        ? 'Las ' + par.petCount + ' mascotas que lo tienen cargado lo conservan guardado, pero no se muestra. '
                        : '') + 'Podés reactivarlo cuando quieras.',
                    confirmLabel: 'Dar de baja',
                    danger: true
                };
            pregunta.cancelLabel = 'Volver';

            servicioConfirmacion.preguntar(pregunta).then(function () {
                servicioApi.post('/api/admin/catalogs/traits/setActive', { pairId: par.pairId, active: activar },
                    avisarYRecargar(activar ? 'Reactivaste el par.' : 'Diste de baja el par.'),
                    function (error) { mostrarError(error, 'No se pudo cambiar el estado.'); });
            });
        };
    }

})(angular.module('walkyDoggy'));

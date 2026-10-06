(function (app) {
    'use strict';

    app.controller('chatModalCtrl', chatModalCtrl);

    chatModalCtrl.$inject = ['$scope', '$modalInstance', '$interval', '$timeout', 'servicioApi', 'servicioNotificaciones', 'datos'];

    //Ventana del chat de una reserva. datos: { bookingKey, rol ('Walker' o 'Customer'), actorId, titulo, subtitulo }.
    //No hay conexion en tiempo real: mientras esta abierta se vuelven a pedir los mensajes cada pocos segundos.
    function chatModalCtrl($scope, $modalInstance, $interval, $timeout, servicioApi, servicioNotificaciones, datos) {
        var segundosEntreActualizaciones = 4;
        var cantidadAnterior = 0;
        var consultando = false;

        $scope.datos = datos;
        $scope.rol = datos.rol;
        $scope.mensajes = [];
        $scope.cargado = false;
        $scope.puedeEscribir = false;
        $scope.enviando = false;
        $scope.errorDeCarga = null;
        $scope.nuevo = { texto: '' };

        cargar();
        var temporizador = $interval(cargar, segundosEntreActualizaciones * 1000);
        $scope.$on('$destroy', function () {
            $interval.cancel(temporizador);
        });

        function parametros() {
            return { params: { bookingKey: datos.bookingKey, actor: datos.rol, actorId: datos.actorId, after: 0 } };
        }

        function cargar() {
            if (consultando) {
                return;
            }
            consultando = true;

            servicioApi.get('/api/messages/list', parametros(), function (resultado) {
                consultando = false;
                $scope.errorDeCarga = null;
                $scope.mensajes = resultado.data.messages;
                $scope.puedeEscribir = resultado.data.canWrite;
                $scope.participantes = { Walker: resultado.data.walker, Customer: resultado.data.customer };
                $scope.cargado = true;

                //Se baja al ultimo mensaje solo cuando llegan mensajes nuevos
                if ($scope.mensajes.length !== cantidadAnterior) {
                    cantidadAnterior = $scope.mensajes.length;
                    bajarAlFinal();
                }
            }, function (error) {
                consultando = false;
                $scope.cargado = true;
                $scope.errorDeCarga = error.data && error.data[0] ? error.data[0] : 'No se pudo cargar el chat.';
            });
        }

        function bajarAlFinal() {
            $timeout(function () {
                var cuerpo = document.getElementById('wd-chat-body');
                if (cuerpo) {
                    cuerpo.scrollTop = cuerpo.scrollHeight;
                }
            });
        }

        //Foto y nombre de quien escribio cada mensaje (el paseador o el cliente)
        $scope.fotoDe = function (mensaje) {
            var persona = $scope.participantes && $scope.participantes[mensaje.senderRole];
            return (persona && persona.profileImage) || '/Content/images/avatar-person.svg';
        };

        $scope.nombreDe = function (mensaje) {
            var persona = $scope.participantes && $scope.participantes[mensaje.senderRole];
            return persona ? persona.name : '';
        };

        $scope.esMio = function (mensaje) {
            return mensaje.senderRole === datos.rol;
        };

        $scope.claseDe = function (mensaje) {
            if (mensaje.senderRole === 'System') {
                return 'is-system';
            }
            return $scope.esMio(mensaje) ? 'is-mine' : 'is-theirs';
        };

        //Marca de dia cuando un mensaje es de otro dia que el anterior
        $scope.cambiaElDia = function (indice) {
            if (indice === 0) {
                return true;
            }
            return moment($scope.mensajes[indice].sentAt).format('YYYY-MM-DD') !== moment($scope.mensajes[indice - 1].sentAt).format('YYYY-MM-DD');
        };

        //Enter envia el mensaje; Mayus+Enter hace un salto de linea
        $scope.alTeclear = function (evento) {
            if (evento.which === 13 && !evento.shiftKey) {
                evento.preventDefault();
                $scope.enviar();
            }
        };

        $scope.enviar = function () {
            var texto = ($scope.nuevo.texto || '').trim();
            if (!texto || $scope.enviando) {
                return;
            }
            $scope.enviando = true;

            var mensaje = { bookingKey: datos.bookingKey, actor: datos.rol, actorId: datos.actorId, text: texto };
            servicioApi.post('/api/messages/send', mensaje, function (resultado) {
                $scope.enviando = false;
                $scope.nuevo.texto = '';
                $scope.mensajes.push(resultado.data);
                cantidadAnterior = $scope.mensajes.length;
                bajarAlFinal();
            }, function (error) {
                $scope.enviando = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo enviar el mensaje.');
            });
        };

        $scope.cerrar = function () {
            $modalInstance.close();
        };
    }

})(angular.module('walkyDoggy'));

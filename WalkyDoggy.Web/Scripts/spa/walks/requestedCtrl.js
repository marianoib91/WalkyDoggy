(function (app) {
    'use strict';

    app.controller('paseosSolicitadosCtrl', paseosSolicitadosCtrl);

    paseosSolicitadosCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioChat', '$rootScope', '$location', '$interval', 'servicioDenuncias'];

    function paseosSolicitadosCtrl($scope, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioChat, $rootScope, $location, $interval, servicioDenuncias) {
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        var idCliente = $rootScope.repository.loggedUser.customerId;
        var etiquetasDeEstrellas = ['Muy malo', 'Malo', 'Regular', 'Bueno', 'Excelente'];
        var horasDeAnticipoParaIniciar = 2;

        $scope.cargado = false;
        $scope.porPagar = [];
        $scope.proximos = [];
        $scope.historial = [];
        $scope.trabajando = false;

        iniciar();
        mostrarResultadoPago();

        //Se actualiza solo cada tanto para ver mensajes nuevos y cambios de estado (por ejemplo, cuando el paseador confirma o termina el paseo)
        var temporizador = $interval(iniciar, 20000);
        $scope.$on('$destroy', function () {
            $interval.cancel(temporizador);
        });

        function iniciar() {
            servicioApi.get('/api/walks/getBookingsForCustomer', { params: { customerId: idCliente } }, function (resultado) {
                var porPagar = [];
                var proximos = [];
                var historial = [];

                angular.forEach(resultado.data, function (reserva) {
                    reserva.start = inicioDe(reserva);

                    if (reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending') {
                        //El paseador ya termino el paseo y falta pagarlo
                        porPagar.push(reserva);
                    } else if (reserva.status !== 'Cancelled' && !reserva.finishedAt) {
                        proximos.push(reserva);
                    } else {
                        historial.push(reserva);
                    }
                });

                //Lo que hay que pagar y lo proximo, del mas cercano al mas lejano; el historial ya viene del mas reciente al mas antiguo
                $scope.porPagar = porPagar.sort(function (a, b) { return a.start - b.start; });
                $scope.proximos = proximos.sort(function (a, b) { return a.start - b.start; });
                $scope.historial = historial;
                $scope.cargado = true;
            });
        }

        function inicioDe(reserva) {
            return moment(reserva.date).hour(Number(reserva.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function mensajeDeError(error, porDefecto) {
            return error.data && error.data[0] ? error.data[0] : porDefecto;
        }

        //Mercado Pago devuelve al cliente a "Paseos solicitados?payment=..." cuando termina de pagar
        function mostrarResultadoPago() {
            var resultado = $location.search().payment;
            if (!resultado) {
                return;
            }

            if (resultado === 'approved') {
                servicioNotificaciones.mostrarExito('¡Pago recibido! El dinero ya está en la cuenta de tu paseador.');
            } else if (resultado === 'pending') {
                servicioNotificaciones.mostrarError('Tu pago todavía no se acreditó. Cuando se confirme lo vas a ver acá. Si Mercado Pago no te funciona, pagale a tu paseador de otra forma y él lo registra.');
            } else if (resultado === 'failed') {
                servicioNotificaciones.mostrarError('El pago no se pudo completar. Podés intentarlo de nuevo con el botón "Pagar".');
            } else {
                servicioNotificaciones.mostrarError('No pudimos verificar el pago. Si ya pagaste, no hace falta hacer nada más: tu paseador lo confirma. Si no, pagale de otra forma y él lo registra.');
            }

            $location.search({}).replace();
        }

        $scope.textoFecha = function (reserva) {
            var fecha = moment(reserva.date);
            return nombresDias[fecha.day()] + ' ' + fecha.format('DD/MM/YYYY');
        };

        $scope.textoHorario = function (reserva) {
            return reserva.timeFrom + ' a ' + (Number(reserva.timeFrom.split(':')[0]) + 1) + ':00';
        };

        $scope.nombresMascotas = function (reserva) {
            return reserva.pets.map(function (mascota) { return mascota.name; }).join(', ');
        };

        //El horario REAL del paseo: el que se registra al iniciarlo (el cliente, al llegar el paseador) y al terminarlo (el paseador)
        $scope.textoPaseoReal = function (reserva) {
            if (!reserva.startedAt) {
                return '';
            }
            var inicio = moment(reserva.startedAt);
            if (!reserva.finishedAt) {
                return 'Inició a las ' + inicio.format('HH:mm');
            }
            var fin = moment(reserva.finishedAt);
            return inicio.format('HH:mm') + ' a ' + fin.format('HH:mm') + ' (' + fin.diff(inicio, 'minutes') + ' min)';
        };

        //El chat se habilita cuando el paseador confirma la reserva; despues de cobrada queda de solo lectura
        $scope.tieneChat = function (reserva) {
            return reserva.status === 'Confirmed' || reserva.messageCount > 0;
        };

        $scope.abrirChat = function (reserva) {
            var actualizar = function () { iniciar(); };

            servicioChat.abrir({
                bookingKey: reserva.bookingKey,
                rol: 'Customer',
                actorId: idCliente,
                titulo: 'Chat con ' + reserva.walkerName,
                subtitulo: $scope.nombresMascotas(reserva) + ' · ' + $scope.textoFecha(reserva) + ', ' + reserva.timeFrom
            }).then(actualizar, actualizar);
        };

        //Solo se denuncia sobre un paseo que el paseador confirmo (la denuncia la ve solo un administrador)
        $scope.puedeDenunciar = function (reserva) {
            return reserva.status === 'Confirmed';
        };

        $scope.denunciar = function (reserva) {
            servicioDenuncias.abrir({
                bookingKey: reserva.bookingKey,
                actor: 'Customer',
                actorId: idCliente,
                titulo: 'Denunciar a ' + reserva.walkerName,
                subtitulo: $scope.nombresMascotas(reserva) + ' · ' + $scope.textoFecha(reserva) + ', ' + reserva.timeFrom
            }).then(angular.noop, angular.noop);
        };

        $scope.textoEstado = function (reserva) {
            if (reserva.status === 'Pending') {
                return 'Esperando confirmación';
            }
            if (reserva.status === 'Confirmed') {
                if (reserva.paymentStatus === 'Received') {
                    return 'Pagado';
                }
                if (reserva.finishedAt) {
                    return reserva.paymentStatus === 'Paid' ? 'Pagado, falta que lo confirme el paseador' : 'Para pagar';
                }
                //El paseo esta en curso desde que se inicia (no desde la hora agendada)
                return reserva.startedAt ? 'En curso' : 'Confirmado';
            }
            if (reserva.cancelledBy === 'Walker') {
                return 'Cancelado por el paseador';
            }
            if (reserva.cancelledBy === 'Customer') {
                return 'Cancelado por vos';
            }
            return 'Sin respuesta del paseador';
        };

        $scope.claseEstado = function (reserva) {
            if (reserva.status === 'Confirmed' && reserva.finishedAt && reserva.paymentStatus === 'Pending') {
                return 'wd-status-pending';
            }
            return 'wd-status-' + reserva.status.toLowerCase();
        };

        $scope.textoPago = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return reserva.paymentStatus === 'Received' ? 'Efectivo · pagado' : 'Efectivo · le pagás en mano al paseador';
            }

            if (reserva.paymentStatus === 'Received') {
                return 'Mercado Pago · pagado';
            }
            if (reserva.paymentStatus === 'Paid') {
                return 'Mercado Pago · pagado';
            }
            return reserva.finishedAt
                ? 'Mercado Pago · pendiente de pago'
                : 'Mercado Pago · pagás online cuando el paseador termine el paseo';
        };

        /* ---------- Cancelar ---------- */

        $scope.puedeCancelar = function (reserva) {
            //Se puede cancelar hasta que se inicia el paseo
            return (reserva.status === 'Pending' || reserva.status === 'Confirmed') && !reserva.startedAt && !reserva.finishedAt;
        };

        $scope.cancelar = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar este paseo?',
                text: 'El paseo con ' + reserva.walkerName + ' del ' + $scope.textoFecha(reserva) + ' a las ' + reserva.timeFrom + ' se va a cancelar.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                var accion = { bookingKey: reserva.bookingKey, actor: 'Customer', actorId: idCliente };
                servicioApi.post('/api/walks/cancel', accion, function () {
                    servicioNotificaciones.mostrarExito('El paseo se canceló.');
                    iniciar();
                }, function (error) {
                    servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo cancelar el paseo.'));
                    iniciar();
                });
            });
        };

        /* ---------- Valorar al paseador ---------- */

        //Se puede valorar un paseo que el paseador ya dio por finalizado y que todavia no se valoro
        $scope.puedeValorar = function (reserva) {
            return reserva.status === 'Confirmed' && !!reserva.finishedAt && !reserva.ratingStars;
        };

        //Lo que el cliente va escribiendo en cada paseo (estrellas, comentario), por reserva: se conserva aunque la lista se actualice sola
        $scope.borradores = {};

        function borradorDe(reserva) {
            return $scope.borradores[reserva.bookingKey] || ($scope.borradores[reserva.bookingKey] = { stars: 0, hover: 0, comment: '', enviando: false });
        }

        //Se tocan las estrellitas y recien ahi se habilita el comentario y el boton de enviar
        $scope.elegirEstrellas = function (reserva, estrellas) {
            borradorDe(reserva).stars = estrellas;
        };

        $scope.pasarSobre = function (reserva, estrellas) {
            borradorDe(reserva).hover = estrellas;
        };

        $scope.estrellasMostradas = function (reserva) {
            var borrador = borradorDe(reserva);
            return borrador.hover || borrador.stars;
        };

        $scope.etiquetaEstrellas = function (estrellas) {
            return estrellas ? etiquetasDeEstrellas[estrellas - 1] : 'Tocá las estrellas para valorar';
        };

        $scope.enviarValoracion = function (reserva) {
            var borrador = borradorDe(reserva);
            if (!borrador.stars || borrador.enviando) {
                return;
            }

            var pedido = { bookingKey: reserva.bookingKey, customerId: idCliente, stars: borrador.stars, comment: (borrador.comment || '').trim() };
            borrador.enviando = true;
            servicioApi.post('/api/ratings/rate', pedido, function () {
                delete $scope.borradores[reserva.bookingKey];
                servicioNotificaciones.mostrarExito('¡Gracias por tu valoración!');
                iniciar();
            }, function (error) {
                borrador.enviando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo enviar la valoración.'));
                iniciar();
            });
        };

        /* ---------- Iniciar el paseo ---------- */

        //El cliente inicia el paseo cuando el paseador llega a buscar a la mascota; se habilita unas horas antes de la hora agendada
        function habilitadoDesde(reserva) {
            return moment(inicioDe(reserva)).subtract(horasDeAnticipoParaIniciar, 'hours');
        }

        $scope.puedeIniciar = function (reserva) {
            return !moment().isBefore(habilitadoDesde(reserva));
        };

        $scope.textoDesdeCuandoIniciar = function (reserva) {
            var desde = habilitadoDesde(reserva);
            return desde.format('HH:mm') + (desde.isSame(moment(), 'day') ? '' : ' del ' + desde.format('DD/MM'));
        };

        $scope.textoQuienInicio = function (reserva) {
            return reserva.startedBy === 'Walker' ? 'lo inició el paseador' : 'lo iniciaste vos';
        };

        $scope.iniciarPaseo = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Ya llegó el paseador?',
                text: 'Al iniciar el paseo queda registrado el horario real de retiro (' + moment().format('HH:mm') + '). Hacelo cuando ' + reserva.walkerName + ' esté ahí para buscar a ' + $scope.nombresMascotas(reserva) + '. Después ya no se puede cancelar.',
                confirmLabel: 'Sí, iniciar paseo',
                cancelLabel: 'Volver'
            }).then(function () {
                var accion = { bookingKey: reserva.bookingKey, actor: 'Customer', actorId: idCliente };

                $scope.trabajando = true;
                servicioApi.post('/api/walks/start', accion, function () {
                    $scope.trabajando = false;
                    servicioNotificaciones.mostrarExito('Paseo iniciado. Cuando ' + reserva.walkerName + ' lo termine, vas a poder pagarlo y valorarlo.');
                    iniciar();
                }, function (error) {
                    $scope.trabajando = false;
                    servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo iniciar el paseo.'));
                    iniciar();
                });
            });
        };

        /* ---------- Pago con Mercado Pago ---------- */

        $scope.esMercadoPago = function (reserva) {
            return reserva.paymentMethod === 'MercadoPago';
        };

        $scope.pagar = function (reserva) {
            $scope.trabajando = true;

            //El servidor crea el pago en Mercado Pago (a nombre del paseador) y devuelve su direccion: ahi el cliente paga y despues vuelve a la app
            servicioApi.post('/api/payments/checkout', { bookingKey: reserva.bookingKey, customerId: idCliente }, function (resultado) {
                window.location.href = resultado.data.url;
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo iniciar el pago.'));
            });
        };
    }

})(angular.module('walkyDoggy'));

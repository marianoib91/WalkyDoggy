(function (app) {
    'use strict';

    app.controller('inicioCtrl', inicioCtrl);

    inicioCtrl.$inject = ['$scope', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioChat', '$rootScope', '$interval', '$location', 'servicioDenuncias'];

    function inicioCtrl($scope, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioChat, $rootScope, $interval, $location, servicioDenuncias) {
        var nombresDias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

        var segundosEntreActualizaciones = 20;

        $scope.userData.mostrarDatosUsuario();
        $scope.roleId = $rootScope.repository.loggedUser.roleId;

        //El administrador no tiene pantalla de inicio de cliente ni de paseador: va a su panel
        if ($scope.roleId == '1') {
            $location.path('/admin');
            return;
        }
        $scope.idPaseador = $rootScope.repository.loggedUser.walkerId;
        $scope.idCliente = $rootScope.repository.loggedUser.customerId;

        $scope.trabajando = false;

        //Paseador: sus reservas segun en que punto del circuito estan
        $scope.pendientes = [];
        $scope.proximos = [];
        $scope.turnosProximos = [];
        $scope.enCurso = [];
        $scope.porCobrar = [];
        $scope.cobrados = [];
        $scope.reservasCargadas = false;

        iniciar();

        function iniciar() {
            //Paseador
            if ($scope.roleId == '3') {
                cargarReservas();

                //Se actualiza solo cada tanto para ver mensajes nuevos y cambios de estado
                var temporizador = $interval(cargarReservas, segundosEntreActualizaciones * 1000);
                $scope.$on('$destroy', function () {
                    $interval.cancel(temporizador);
                });

                //Se consulta el perfil para avisar si todavia no vinculo su cuenta de Mercado Pago
                var idUsuario = $rootScope.repository.loggedUser.id;
                servicioApi.get('/api/walkers/getByUserId', { params: { userId: idUsuario } }, function (resultado) {
                    $scope.perfilPaseador = resultado.data;
                });
            }
        }

        /* ---------- Paseador: solicitudes, paseos y cobros ---------- */

        //Montos al estilo argentino (4321.5 -> 4.321,50)
        function formatearMonto(monto) {
            return Number(monto).toLocaleString('es-AR', { minimumFractionDigits: monto % 1 ? 2 : 0, maximumFractionDigits: 2 });
        }

        function inicioDe(reserva) {
            return moment(reserva.date).hour(Number(reserva.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function cargarReservas() {
            servicioApi.get('/api/walks/getBookingsForWalker', { params: { walkerId: $scope.idPaseador } }, function (resultado) {
                var pendientes = [], proximos = [], enCurso = [], porCobrar = [], cobrados = [];

                angular.forEach(resultado.data, function (reserva) {
                    if (reserva.status === 'Pending') {
                        pendientes.push(reserva);
                    } else if (reserva.status === 'Confirmed') {
                        if (reserva.receivedAt) {
                            cobrados.push(reserva);
                        } else if (reserva.finishedAt) {
                            porCobrar.push(reserva);
                        } else if (reserva.startedAt) {
                            enCurso.push(reserva);
                        } else {
                            proximos.push(reserva);
                        }
                    }
                });

                $scope.pendientes = pendientes;
                $scope.proximos = proximos;
                $scope.turnosProximos = agruparEnTurnos(proximos.concat(pendientes));
                $scope.enCurso = enCurso;
                $scope.porCobrar = porCobrar;
                $scope.cobrados = cobrados.sort(function (a, b) { return new Date(b.receivedAt) - new Date(a.receivedAt); });
                $scope.reservasCargadas = true;
            });
        }

        //Un turno junta todas las reservas (confirmadas y por confirmar) del mismo dia y horario.
        //Dentro del turno hay un bloque por cliente (una casa, un chat, un inicio de paseo) con sus mascotas,
        //asi el paseador ve de un vistazo a quien tiene que buscar y donde
        function agruparEnTurnos(reservas) {
            var turnos = {};

            angular.forEach(reservas, function (reserva) {
                var clave = moment(reserva.date).format('YYYY-MM-DD') + ' ' + reserva.timeFrom;
                var turno = turnos[clave] || (turnos[clave] = { clave: clave, inicio: inicioDe(reserva), reserva: reserva, clientes: [] });
                turno.clientes.push({ clave: reserva.bookingKey, reserva: reserva });
            });

            return Object.keys(turnos).map(function (clave) { return turnos[clave]; })
                .sort(function (a, b) { return a.inicio - b.inicio; });
        }

        //Que clientes del turno estan desplegados (por clave de texto: la lista se recarga sola y los objetos cambian)
        $scope.clientesAbiertos = {};

        $scope.alternarCliente = function (cliente) {
            $scope.clientesAbiertos[cliente.clave] = !$scope.clientesAbiertos[cliente.clave];
        };

        function cantidadDePerros(clientes) {
            return clientes.reduce(function (total, cliente) { return total + cliente.reserva.pets.length; }, 0);
        }

        $scope.textoCantidadPerros = function (turno) {
            var cantidad = cantidadDePerros(turno.clientes);
            return cantidad + (cantidad === 1 ? ' perro' : ' perros');
        };

        $scope.esPendiente = function (cliente) {
            return cliente.reserva.status === 'Pending';
        };

        //Perros de las solicitudes que todavia esperan respuesta
        $scope.cantidadPorConfirmar = function (turno) {
            return cantidadDePerros(turno.clientes.filter($scope.esPendiente));
        };

        //Cada cliente tiene una sola conversacion, aunque lleve varias mascotas
        $scope.mensajesSinLeerDelTurno = function (turno) {
            return turno.clientes.reduce(function (total, cliente) { return total + (cliente.reserva.unreadMessages || 0); }, 0);
        };

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

        //El horario REAL del paseo, el que registra el paseador al iniciarlo y al terminarlo
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

        $scope.textoPago = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return reserva.paymentStatus === 'Received' ? 'Efectivo · cobrado' : 'Efectivo · te paga en mano al terminar';
            }

            if (reserva.paymentStatus === 'Received') {
                return 'Mercado Pago · cobrado';
            }
            if (reserva.paymentStatus === 'Paid') {
                return 'Mercado Pago · el cliente ya pagó, el dinero está en tu cuenta';
            }
            return reserva.finishedAt
                ? 'Mercado Pago · esperando que el cliente pague'
                : 'Mercado Pago · el cliente te paga online cuando termines el paseo';
        };

        $scope.textoEstadoCobro = function (reserva) {
            if (reserva.paymentMethod !== 'MercadoPago') {
                return 'Cobrar en efectivo';
            }
            return reserva.paymentStatus === 'Paid' ? 'Pagado' : 'Esperando el pago';
        };

        //En efectivo se confirma directamente; con Mercado Pago, cuando el cliente ya pago
        $scope.puedeRecibir = function (reserva) {
            return reserva.paymentMethod !== 'MercadoPago' || reserva.paymentStatus === 'Paid';
        };

        /* ---------- Chat con el cliente ---------- */

        //El chat se habilita al confirmar la reserva; despues de cobrada queda de solo lectura
        $scope.tieneChat = function (reserva) {
            return reserva.status === 'Confirmed' || reserva.messageCount > 0;
        };

        $scope.abrirChat = function (reserva) {
            var actualizar = function () { cargarReservas(); };

            servicioChat.abrir({
                bookingKey: reserva.bookingKey,
                rol: 'Walker',
                actorId: $scope.idPaseador,
                titulo: 'Chat con ' + reserva.customerFullName,
                subtitulo: $scope.nombresMascotas(reserva) + ' · ' + $scope.textoFecha(reserva) + ', ' + reserva.timeFrom
            }).then(actualizar, actualizar);
        };

        /* ---------- Denuncias ---------- */

        //El paseador denuncia al cliente de una reserva confirmada (la denuncia la ve solo un administrador)
        $scope.denunciar = function (reserva) {
            servicioDenuncias.abrir({
                bookingKey: reserva.bookingKey,
                actor: 'Walker',
                actorId: $scope.idPaseador,
                titulo: 'Denunciar a ' + reserva.customerFullName,
                subtitulo: $scope.nombresMascotas(reserva) + ' · ' + $scope.textoFecha(reserva) + ', ' + reserva.timeFrom
            }).then(angular.noop, angular.noop);
        };

        /* ---------- Reseñas de las mascotas ---------- */

        var etiquetasDeEstrellas = ['Muy malo', 'Malo', 'Regular', 'Bueno', 'Excelente'];

        //Reseñas que dejaron otros paseadores de cada mascota (por id de mascota), cargadas cuando se abre el detalle
        $scope.resenasDe = {};
        $scope.resenasAbiertas = {};

        //"4,5" (promedio de estrellas al estilo argentino)
        $scope.textoPromedio = function (promedio) {
            return Number(promedio).toFixed(1).replace('.', ',');
        };

        //Hay mas de una reseña negativa, o la mitad o mas de las reseñas lo son: se avisa en rojo para que el paseador lo tenga en cuenta
        $scope.esAlertaDeResenas = function (mascota) {
            return mascota.negativeReviews > 0 && mascota.negativeReviews * 2 >= mascota.reviewCount;
        };

        $scope.alternarResenas = function (mascota) {
            if ($scope.resenasAbiertas[mascota.id]) {
                $scope.resenasAbiertas[mascota.id] = false;
                return;
            }

            $scope.resenasAbiertas[mascota.id] = true;
            servicioApi.get('/api/petReviews/list', { params: { petId: mascota.id, walkerId: $scope.idPaseador } }, function (resultado) {
                //Las propias primero (van destacadas) y despues las demas, de la mas reciente a la mas antigua
                $scope.resenasDe[mascota.id] = resultado.data.reviews.slice().sort(function (a, b) {
                    return (b.isMine - a.isMine) || (new Date(b.date) - new Date(a.date));
                });
            }, function (error) {
                $scope.resenasAbiertas[mascota.id] = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudieron cargar las reseñas.');
            });
        };

        //Lo que el paseador va escribiendo para cada mascota de cada paseo (estrellas y comentario): se conserva aunque la lista se actualice sola
        $scope.borradores = {};

        function borradorDe(reserva, mascota) {
            var clave = reserva.bookingKey + '|' + mascota.id;
            return $scope.borradores[clave] || ($scope.borradores[clave] = { stars: 0, hover: 0, comment: '', enviando: false });
        }

        //Las mascotas de un paseo finalizado que el paseador todavia no reseño
        $scope.mascotasSinResenar = function (reserva) {
            return reserva.pets.filter(function (mascota) { return !mascota.reviewedByWalker; });
        };

        $scope.mascotasResenadas = function (reserva) {
            return reserva.pets.filter(function (mascota) { return mascota.reviewedByWalker; });
        };

        $scope.borradorDe = borradorDe;

        //Se tocan las estrellitas y recien ahi se habilita el comentario y el boton de enviar
        $scope.elegirEstrellas = function (reserva, mascota, estrellas) {
            borradorDe(reserva, mascota).stars = estrellas;
        };

        $scope.pasarSobre = function (reserva, mascota, estrellas) {
            borradorDe(reserva, mascota).hover = estrellas;
        };

        $scope.estrellasMostradas = function (reserva, mascota) {
            var borrador = borradorDe(reserva, mascota);
            return borrador.hover || borrador.stars;
        };

        $scope.etiquetaEstrellas = function (estrellas) {
            return estrellas ? etiquetasDeEstrellas[estrellas - 1] : 'Tocá las estrellas para valorar';
        };

        $scope.enviarResena = function (reserva, mascota) {
            var borrador = borradorDe(reserva, mascota);
            if (!borrador.stars || borrador.enviando) {
                return;
            }

            var pedido = { bookingKey: reserva.bookingKey, walkerId: $scope.idPaseador, petId: mascota.id, stars: borrador.stars, comment: (borrador.comment || '').trim() };
            borrador.enviando = true;
            servicioApi.post('/api/petReviews/rate', pedido, function () {
                delete $scope.borradores[reserva.bookingKey + '|' + mascota.id];
                delete $scope.resenasDe[mascota.id];
                servicioNotificaciones.mostrarExito('Listo, tu reseña de ' + mascota.name + ' quedó guardada. Les va a servir a otros paseadores.');
                cargarReservas();
            }, function (error) {
                borrador.enviando = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo guardar la reseña.');
                cargarReservas();
            });
        };

        /* ---------- Acciones sobre las reservas ---------- */

        $scope.confirmar = function (reserva) {
            enviarAccion('/api/walks/confirm', reserva, 'Confirmaste el paseo. Ya pueden ponerse de acuerdo por el chat en el horario de retiro.');
        };

        $scope.rechazar = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Rechazar la solicitud?',
                text: 'La solicitud de ' + reserva.customerFullName + ' para ' + $scope.nombresMascotas(reserva) + ' se va a cancelar.',
                confirmLabel: 'Rechazar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/walks/cancel', reserva, 'Rechazaste la solicitud.');
            });
        };

        $scope.cancelarConfirmado = function (reserva) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar el paseo confirmado?',
                text: 'El paseo de ' + $scope.nombresMascotas(reserva) + ' del ' + $scope.textoFecha(reserva) + ' a las ' + reserva.timeFrom + ' se va a cancelar y el cliente lo va a ver como cancelado.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/walks/cancel', reserva, 'El paseo se canceló.');
            });
        };

        //El paseo lo inicia siempre el cliente (el paseador no puede), asi que el horario real de inicio es confiable
        $scope.textoQuienInicio = function () {
            return 'lo inició el cliente';
        };

        $scope.finalizar = function (reserva) {
            var notaDePago = reserva.paymentMethod === 'MercadoPago'
                ? 'El cliente va a ver el botón para pagarte por Mercado Pago.'
                : 'El cliente te paga en efectivo.';

            servicioConfirmacion.preguntar({
                title: '¿Terminaste el paseo?',
                text: 'Confirmá que ya devolviste a ' + $scope.nombresMascotas(reserva) + '. ' + notaDePago,
                confirmLabel: 'Sí, terminé',
                cancelLabel: 'Volver'
            }).then(function () {
                enviarAccion('/api/walks/finish', reserva, 'Diste el paseo por finalizado. Ahora el cliente puede pagarte.');
            });
        };

        $scope.recibir = function (reserva) {
            var texto = reserva.paymentMethod === 'MercadoPago'
                ? 'Confirmá que el pago de $' + formatearMonto(reserva.total) + ' de ' + reserva.customerFullName + ' por Mercado Pago ya figura en tu cuenta.'
                : 'Confirmá que ' + reserva.customerFullName + ' te pagó $' + formatearMonto(reserva.total) + ' en efectivo.';

            servicioConfirmacion.preguntar({
                title: '¿Recibiste el pago?',
                text: texto,
                confirmLabel: 'Sí, lo recibí',
                cancelLabel: 'Volver'
            }).then(function () {
                enviarAccion('/api/walks/receive', reserva, 'Listo, el paseo quedó cobrado y cerrado.');
            });
        };

        function enviarAccion(url, reserva, mensajeDeExito) {
            var accion = { bookingKey: reserva.bookingKey, actor: 'Walker', actorId: $scope.idPaseador };

            $scope.trabajando = true;
            servicioApi.post(url, accion, function () {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarExito(mensajeDeExito);
                cargarReservas();
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(error.data && error.data[0] ? error.data[0] : 'No se pudo completar la acción.');
                cargarReservas();
            });
        }
    }

})(angular.module('walkyDoggy'));

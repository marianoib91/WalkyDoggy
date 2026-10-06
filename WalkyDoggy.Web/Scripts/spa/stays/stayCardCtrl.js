(function (app) {
    'use strict';

    app.controller('tarjetaHospedajeCtrl', tarjetaHospedajeCtrl);

    tarjetaHospedajeCtrl.$inject = ['$scope', '$rootScope', '$window', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion', 'servicioChat'];

    //La tarjeta de un hospedaje (scope.hospedaje, que viene del ng-repeat) con todo lo que se puede hacer desde ella.
    //Se usa tanto en los turnos del cuidador como en "Mis hospedajes" del cliente. La pantalla que la contiene define
    //recargarHospedajes() para volver a pedir la lista despues de cada accion.
    function tarjetaHospedajeCtrl($scope, $rootScope, $window, servicioApi, servicioNotificaciones, servicioConfirmacion, servicioChat) {
        var usuario = $rootScope.repository.loggedUser;
        var etiquetasDeEstrellas = ['Muy malo', 'Malo', 'Regular', 'Bueno', 'Excelente'];

        $scope.esPaseador = usuario.roleId == '3';
        $scope.idPaseador = usuario.walkerId;
        $scope.idCliente = usuario.customerId;
        $scope.trabajando = false;

        var textosDeEstado = {
            Pending: 'Esperando confirmación',
            Upcoming: 'Confirmado',
            InProgress: 'En curso',
            ToCollect: 'Para pagar',
            Collected: 'Finalizado y pagado',
            Cancelled: 'Cancelado'
        };

        function recargar() {
            if ($scope.recargarHospedajes) {
                $scope.recargarHospedajes();
            }
        }

        function mensajeDeError(error, porDefecto) {
            return error && error.data && error.data[0] ? error.data[0] : porDefecto;
        }

        /* ---------- Textos ---------- */

        $scope.textoEstado = function (hospedaje) {
            if (hospedaje.status === 'ToCollect' && hospedaje.paymentMethod === 'MercadoPago' && hospedaje.paymentStatus === 'Paid') {
                return $scope.esPaseador ? 'Pagado: confirmalo' : 'Pagado';
            }
            return textosDeEstado[hospedaje.status] || hospedaje.status;
        };

        //Los colores reutilizan los de las reservas: amarillo (espera), verde (va bien), rojo (cancelado)
        $scope.claseEstado = function (hospedaje) {
            return hospedaje.status === 'Pending' || hospedaje.status === 'ToCollect' ? 'wd-status-pending'
                 : hospedaje.status === 'Cancelled' ? 'wd-status-cancelled'
                 : 'wd-status-confirmed';
        };

        $scope.fecha = function (fecha) {
            return moment(fecha).format('DD/MM/YYYY');
        };

        $scope.dinero = function (monto) {
            return '$' + Number(monto).toLocaleString('es-AR', { minimumFractionDigits: monto % 1 ? 2 : 0, maximumFractionDigits: 2 });
        };

        $scope.nombresDeMascotas = function (hospedaje) {
            var nombres = hospedaje.pets.map(function (mascota) { return mascota.name; });
            return nombres.length > 1 ? nombres.slice(0, -1).join(', ') + ' y ' + nombres[nombres.length - 1] : nombres[0];
        };

        $scope.textoNoches = function (hospedaje) {
            return hospedaje.nights + (hospedaje.nights === 1 ? ' noche' : ' noches');
        };

        $scope.textoPerros = function (cantidad) {
            return cantidad + (cantidad === 1 ? ' perro' : ' perros');
        };

        $scope.esMercadoPago = function (hospedaje) {
            return hospedaje.paymentMethod === 'MercadoPago';
        };

        $scope.textoPago = function (hospedaje) {
            return hospedaje.paymentMethod === 'MercadoPago' ? 'Mercado Pago' : 'Efectivo';
        };

        /* ---------- Acciones ---------- */

        function enviarAccion(url, hospedaje, actor, actorId, mensajeDeExito) {
            $scope.trabajando = true;
            servicioApi.post(url, { bookingKey: hospedaje.bookingKey, actor: actor, actorId: actorId }, function () {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarExito(mensajeDeExito);
                recargar();
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo completar la acción.'));
                recargar();
            });
        }

        $scope.abrirChat = function (hospedaje) {
            var comoPaseador = $scope.esPaseador;
            var actualizar = recargar;

            servicioChat.abrir({
                bookingKey: hospedaje.bookingKey,
                rol: comoPaseador ? 'Walker' : 'Customer',
                actorId: comoPaseador ? $scope.idPaseador : $scope.idCliente,
                titulo: 'Chat con ' + (comoPaseador ? hospedaje.customerName : hospedaje.walkerName),
                subtitulo: 'Hospedaje de ' + $scope.nombresDeMascotas(hospedaje) + ' · ' + $scope.fecha(hospedaje.checkIn) + ' al ' + $scope.fecha(hospedaje.checkOut)
            }).then(actualizar, actualizar);
        };

        $scope.confirmar = function (hospedaje) {
            enviarAccion('/api/stays/confirm', hospedaje, 'Walker', $scope.idPaseador, 'Confirmaste el hospedaje. Coordinen por el chat cómo y a qué hora entregar a los perros.');
        };

        $scope.rechazar = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Rechazar el pedido?',
                text: 'El pedido de ' + hospedaje.customerName + ' para ' + $scope.nombresDeMascotas(hospedaje) + ' se va a cancelar.',
                confirmLabel: 'Rechazar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/stays/cancel', hospedaje, 'Walker', $scope.idPaseador, 'Rechazaste el pedido.');
            });
        };

        $scope.cancelarComoCuidador = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar el hospedaje?',
                text: 'Hasta que recibas a los perros podés cancelar. ' + hospedaje.customerName + ' lo va a ver como cancelado.',
                confirmLabel: 'Cancelar hospedaje',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/stays/cancel', hospedaje, 'Walker', $scope.idPaseador, 'El hospedaje se canceló.');
            });
        };

        //Se puede recibir a los perros desde el dia de ingreso
        $scope.puedeRecibir = function (hospedaje) {
            return hospedaje.status === 'Upcoming' && !moment(hospedaje.checkIn).isAfter(moment(), 'day');
        };

        $scope.recibirPerros = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Recibiste a los perros?',
                text: 'Desde ese momento comienza el hospedaje de ' + $scope.nombresDeMascotas(hospedaje) + ' y ya no se puede cancelar.',
                confirmLabel: 'Sí, los recibí',
                cancelLabel: 'Todavía no'
            }).then(function () {
                enviarAccion('/api/stays/start', hospedaje, 'Walker', $scope.idPaseador, 'Registraste que recibiste a los perros.');
            });
        };

        $scope.devolverPerros = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Devolviste a los perros?',
                text: 'Se termina el hospedaje y ' + hospedaje.customerName + ' ve cuánto tiene que pagarte (' + $scope.dinero(hospedaje.total) + ').',
                confirmLabel: 'Sí, los devolví',
                cancelLabel: 'Todavía no'
            }).then(function () {
                enviarAccion('/api/stays/finish', hospedaje, 'Walker', $scope.idPaseador, 'Terminó el hospedaje. Cuando te paguen, confirmalo acá.');
            });
        };

        //El cuidador siempre confirma el cobro, con cualquier forma de pago (si Mercado Pago no funciona, el cliente le paga de otra forma)
        $scope.puedeConfirmarCobro = function (hospedaje) {
            return hospedaje.status === 'ToCollect';
        };

        $scope.confirmarCobro = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Ya te pagaron?',
                text: 'Confirmá que recibiste ' + $scope.dinero(hospedaje.total) + ' de ' + hospedaje.customerName + '.',
                confirmLabel: 'Sí, me pagaron',
                cancelLabel: 'Todavía no'
            }).then(function () {
                enviarAccion('/api/stays/receive', hospedaje, 'Walker', $scope.idPaseador, 'Quedó registrado el cobro.');
            });
        };

        $scope.cancelarComoCliente = function (hospedaje) {
            servicioConfirmacion.preguntar({
                title: '¿Cancelar el hospedaje?',
                text: 'Hasta que el cuidador reciba a los perros podés cancelar sin problema.',
                confirmLabel: 'Cancelar hospedaje',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                enviarAccion('/api/stays/cancel', hospedaje, 'Customer', $scope.idCliente, 'El hospedaje se canceló.');
            });
        };

        $scope.puedeCancelarCliente = function (hospedaje) {
            return hospedaje.status === 'Pending' || hospedaje.status === 'Upcoming';
        };

        /* ---------- Pago (cliente) ---------- */

        //Copia un texto al portapapeles (con una alternativa para navegadores sin la API moderna)
        $scope.copiar = function (texto, que) {
            var listo = function () { servicioNotificaciones.mostrarExito('Copiaste ' + que + '.'); };
            var alternativa = function () {
                var campo = $window.document.createElement('textarea');
                campo.value = texto;
                campo.setAttribute('readonly', '');
                campo.style.position = 'fixed';
                campo.style.opacity = '0';
                $window.document.body.appendChild(campo);
                campo.select();
                try { $window.document.execCommand('copy'); listo(); } catch (e) { servicioNotificaciones.mostrarError('No se pudo copiar. Seleccionalo y copialo a mano.'); }
                $window.document.body.removeChild(campo);
            };

            if ($window.navigator.clipboard && $window.navigator.clipboard.writeText) {
                $window.navigator.clipboard.writeText(texto).then(listo, alternativa);
            } else {
                alternativa();
            }
        };

        $scope.pagar = function (hospedaje) {
            $scope.trabajando = true;

            //El servidor crea el pago en Mercado Pago (a nombre del cuidador) y devuelve su direccion: ahi el cliente paga y despues vuelve a la app
            servicioApi.post('/api/payments/checkout', { bookingKey: hospedaje.bookingKey, customerId: $scope.idCliente }, function (resultado) {
                $window.location.href = resultado.data.url;
            }, function (error) {
                $scope.trabajando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo iniciar el pago.'));
            });
        };

        /* ---------- Valoraciones ---------- */

        //Lo que se va escribiendo en cada valoracion (estrellas, comentario): se conserva aunque la lista se actualice sola
        $scope.borrador = { stars: 0, hover: 0, comment: "", enviando: false };
        $scope.borradoresDeMascotas = {};

        $scope.etiquetaEstrellas = function (estrellas) {
            return estrellas ? etiquetasDeEstrellas[estrellas - 1] : 'Tocá las estrellas para valorar';
        };

        //El cliente valora al cuidador cuando devolvio a los perros
        $scope.puedeValorar = function (hospedaje) {
            return !$scope.esPaseador && (hospedaje.status === 'ToCollect' || hospedaje.status === 'Collected') && !hospedaje.ratingStars;
        };

        $scope.estrellasMostradas = function () {
            return $scope.borrador.hover || $scope.borrador.stars;
        };

        $scope.elegirEstrellas = function (estrellas) { $scope.borrador.stars = estrellas; };
        $scope.pasarSobre = function (estrellas) { $scope.borrador.hover = estrellas; };

        $scope.enviarValoracion = function (hospedaje) {
            var borrador = $scope.borrador;
            if (!borrador.stars || borrador.enviando) {
                return;
            }

            borrador.enviando = true;
            servicioApi.post('/api/ratings/rate', {
                bookingKey: hospedaje.bookingKey,
                customerId: $scope.idCliente,
                stars: borrador.stars,
                comment: (borrador.comment || '').trim()
            }, function () {
                servicioNotificaciones.mostrarExito('¡Gracias por tu valoración!');
                recargar();
            }, function (error) {
                borrador.enviando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo enviar la valoración.'));
            });
        };

        //El cuidador reseña a cada perro cuando los devolvio
        $scope.mascotasSinResenar = function (hospedaje) {
            if (!$scope.esPaseador || (hospedaje.status !== 'ToCollect' && hospedaje.status !== 'Collected')) {
                return [];
            }
            return hospedaje.pets.filter(function (mascota) { return !mascota.reviewedByWalker; });
        };

        $scope.mascotasResenadas = function (hospedaje) {
            return $scope.esPaseador ? hospedaje.pets.filter(function (mascota) { return mascota.reviewedByWalker; }) : [];
        };

        $scope.borradorDe = function (mascota) {
            return $scope.borradoresDeMascotas[mascota.id] || ($scope.borradoresDeMascotas[mascota.id] = { stars: 0, hover: 0, comment: '', enviando: false });
        };

        $scope.estrellasDeMascota = function (mascota) {
            var borrador = $scope.borradorDe(mascota);
            return borrador.hover || borrador.stars;
        };

        $scope.elegirEstrellasDeMascota = function (mascota, estrellas) { $scope.borradorDe(mascota).stars = estrellas; };
        $scope.pasarSobreMascota = function (mascota, estrellas) { $scope.borradorDe(mascota).hover = estrellas; };

        $scope.enviarResena = function (hospedaje, mascota) {
            var borrador = $scope.borradorDe(mascota);
            if (!borrador.stars || borrador.enviando) {
                return;
            }

            borrador.enviando = true;
            servicioApi.post('/api/petReviews/rate', {
                bookingKey: hospedaje.bookingKey,
                walkerId: $scope.idPaseador,
                petId: mascota.id,
                stars: borrador.stars,
                comment: (borrador.comment || '').trim()
            }, function () {
                servicioNotificaciones.mostrarExito('Listo, tu reseña de ' + mascota.name + ' quedó guardada. Les va a servir a otros cuidadores y paseadores.');
                recargar();
            }, function (error) {
                borrador.enviando = false;
                servicioNotificaciones.mostrarError(mensajeDeError(error, 'No se pudo guardar la reseña.'));
            });
        };
    }

})(angular.module('walkyDoggy'));

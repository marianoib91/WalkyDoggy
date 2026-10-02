(function (app) {
    'use strict';

    app.controller('indexCtrl', indexCtrl);

    indexCtrl.$inject = ['$scope', 'apiService', 'notificationService', 'confirmService', '$rootScope'];

    function indexCtrl($scope, apiService, notificationService, confirmService, $rootScope) {
        var dayNames = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

        $scope.userData.displayUserInfo();
        $scope.roleId = $rootScope.repository.loggedUser.roleId;
        $scope.walkerId = $rootScope.repository.loggedUser.walkerId;
        $scope.customerId = $rootScope.repository.loggedUser.customerId;

        $scope.walkers = {};
        $scope.hasDistances = false;
        $scope.pendingPayments = 0;
        $scope.working = false;

        //Paseador: sus reservas segun en que punto del circuito estan
        $scope.pending = [];
        $scope.toFinish = [];
        $scope.upcoming = [];
        $scope.toCollect = [];
        $scope.collected = [];
        $scope.loadedBookings = false;

        init();

        function init() {
            //Cliente
            if ($scope.roleId == '2') {
                //Los paseadores llegan ordenados por cercania al domicilio del cliente
                apiService.get('/api/walkers/getAllOrderedByDistance', { params: { customerId: $scope.customerId } }, onLoadWalkersCompleted);

                //Se avisa si hay paseos terminados que todavia no pago
                apiService.get('/api/walks/getBookingsForCustomer', { params: { customerId: $scope.customerId } }, function (result) {
                    $scope.pendingPayments = result.data.filter(function (booking) {
                        return booking.status === 'Confirmed' && booking.finishedAt && booking.paymentStatus === 'Pending';
                    }).length;
                });
            }

            //Paseador
            if ($scope.roleId == '3') {
                loadBookings();

                //Se consulta el perfil para avisar si todavia no vinculo su cuenta de Mercado Pago
                var userId = $rootScope.repository.loggedUser.id;
                apiService.get('/api/walkers/getByUserId', { params: { userId: userId } }, function (result) {
                    $scope.walkerProfile = result.data;
                });
            }
        }

        function onLoadWalkersCompleted(result) {
            $scope.walkers = result.data;
            $scope.hasDistances = result.data.some(function (walker) {
                return walker.distanceKm !== null && walker.distanceKm !== undefined;
            });

            //Por defecto, los mas cercanos; si el cliente no tiene domicilio ubicado en el mapa, los mejor valorados
            $scope.setSort($scope.hasDistances ? 'distance' : 'rating');
        }

        /* ---------- Cliente: orden de la lista de paseadores ---------- */

        function hasDistance(walker) {
            return walker.distanceKm !== null && walker.distanceKm !== undefined;
        }

        //distance: de menor a mayor distancia (los que no tienen domicilio ubicado, al final).
        //rating: de mayor a menor promedio; a igual promedio, el que tiene mas valoraciones; los que no tienen, al final.
        $scope.setSort = function (sortBy) {
            $scope.sortBy = sortBy;

            var list = ($scope.walkers.slice ? $scope.walkers.slice() : []);
            list.sort(function (a, b) {
                if (sortBy === 'distance') {
                    if (hasDistance(a) !== hasDistance(b)) { return hasDistance(a) ? -1 : 1; }
                    return (a.distanceKm || 0) - (b.distanceKm || 0);
                }

                var ratedA = a.averageRating !== null && a.averageRating !== undefined;
                var ratedB = b.averageRating !== null && b.averageRating !== undefined;
                if (ratedA !== ratedB) { return ratedA ? -1 : 1; }
                if (ratedA && a.averageRating !== b.averageRating) { return b.averageRating - a.averageRating; }
                if (a.ratingCount !== b.ratingCount) { return b.ratingCount - a.ratingCount; }
                if (hasDistance(a) && hasDistance(b)) { return a.distanceKm - b.distanceKm; }
                return 0;
            });

            $scope.orderedWalkers = list;
        };

        /* ---------- Paseador: solicitudes, paseos y cobros ---------- */

        //Montos al estilo argentino (4321.5 -> 4.321,50)
        function money(amount) {
            return Number(amount).toLocaleString('es-AR', { minimumFractionDigits: amount % 1 ? 2 : 0, maximumFractionDigits: 2 });
        }

        function startOf(booking) {
            return moment(booking.date).hour(Number(booking.timeFrom.split(':')[0])).minute(0).second(0).toDate();
        }

        function loadBookings() {
            apiService.get('/api/walks/getBookingsForWalker', { params: { walkerId: $scope.walkerId } }, function (result) {
                var now = new Date();
                var pending = [], toFinish = [], upcoming = [], toCollect = [], collected = [];

                angular.forEach(result.data, function (booking) {
                    if (booking.status === 'Pending') {
                        pending.push(booking);
                    } else if (booking.status === 'Confirmed') {
                        if (booking.receivedAt) {
                            collected.push(booking);
                        } else if (booking.finishedAt) {
                            toCollect.push(booking);
                        } else if (startOf(booking) <= now) {
                            toFinish.push(booking);
                        } else {
                            upcoming.push(booking);
                        }
                    }
                });

                $scope.pending = pending;
                $scope.toFinish = toFinish;
                $scope.upcoming = upcoming;
                $scope.toCollect = toCollect;
                $scope.collected = collected.sort(function (a, b) { return new Date(b.receivedAt) - new Date(a.receivedAt); });
                $scope.loadedBookings = true;
            });
        }

        $scope.dateText = function (booking) {
            var date = moment(booking.date);
            return dayNames[date.day()] + ' ' + date.format('DD/MM/YYYY');
        };

        $scope.timeText = function (booking) {
            return booking.timeFrom + ' a ' + (Number(booking.timeFrom.split(':')[0]) + 1) + ':00';
        };

        $scope.petNames = function (booking) {
            return booking.pets.map(function (pet) { return pet.name; }).join(', ');
        };

        $scope.paymentText = function (booking) {
            if (booking.paymentMethod !== 'MercadoPago') {
                return booking.paymentStatus === 'Received' ? 'Efectivo · cobrado' : 'Efectivo · te paga en mano al terminar';
            }

            if (booking.paymentStatus === 'Received') {
                return 'Mercado Pago · cobrado';
            }
            if (booking.paymentStatus === 'Paid') {
                return 'Mercado Pago · el cliente ya pagó, el dinero está en tu cuenta';
            }
            return booking.finishedAt
                ? 'Mercado Pago · esperando que el cliente pague'
                : 'Mercado Pago · el cliente te paga online cuando termines el paseo';
        };

        $scope.collectStatusText = function (booking) {
            if (booking.paymentMethod !== 'MercadoPago') {
                return 'Cobrar en efectivo';
            }
            return booking.paymentStatus === 'Paid' ? 'Pagado' : 'Esperando el pago';
        };

        //En efectivo se confirma directamente; con Mercado Pago, cuando el cliente ya pago
        $scope.canReceive = function (booking) {
            return booking.paymentMethod !== 'MercadoPago' || booking.paymentStatus === 'Paid';
        };

        $scope.confirm = function (booking) {
            send('/api/walks/confirm', booking, 'Confirmaste el paseo. El cliente ya lo ve como confirmado.');
        };

        $scope.reject = function (booking) {
            confirmService.ask({
                title: '¿Rechazar la solicitud?',
                text: 'La solicitud de ' + booking.customerFullName + ' para ' + $scope.petNames(booking) + ' se va a cancelar.',
                confirmLabel: 'Rechazar',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                send('/api/walks/cancel', booking, 'Rechazaste la solicitud.');
            });
        };

        $scope.cancelConfirmed = function (booking) {
            confirmService.ask({
                title: '¿Cancelar el paseo confirmado?',
                text: 'El paseo de ' + $scope.petNames(booking) + ' del ' + $scope.dateText(booking) + ' a las ' + booking.timeFrom + ' se va a cancelar y el cliente lo va a ver como cancelado.',
                confirmLabel: 'Cancelar paseo',
                cancelLabel: 'Volver',
                danger: true
            }).then(function () {
                send('/api/walks/cancel', booking, 'El paseo se canceló.');
            });
        };

        $scope.finish = function (booking) {
            var payNote = booking.paymentMethod === 'MercadoPago'
                ? 'El cliente va a ver el botón para pagarte por Mercado Pago.'
                : 'El cliente te paga en efectivo.';

            confirmService.ask({
                title: '¿Terminaste el paseo?',
                text: 'Confirmá que ya devolviste a ' + $scope.petNames(booking) + '. ' + payNote,
                confirmLabel: 'Sí, terminé',
                cancelLabel: 'Volver'
            }).then(function () {
                send('/api/walks/finish', booking, 'Diste el paseo por finalizado. Ahora el cliente puede pagarte.');
            });
        };

        $scope.receive = function (booking) {
            var text = booking.paymentMethod === 'MercadoPago'
                ? 'Confirmá que el pago de $' + money(booking.total) + ' de ' + booking.customerFullName + ' por Mercado Pago ya figura en tu cuenta.'
                : 'Confirmá que ' + booking.customerFullName + ' te pagó $' + money(booking.total) + ' en efectivo.';

            confirmService.ask({
                title: '¿Recibiste el pago?',
                text: text,
                confirmLabel: 'Sí, lo recibí',
                cancelLabel: 'Volver'
            }).then(function () {
                send('/api/walks/receive', booking, 'Listo, el paseo quedó cobrado y cerrado.');
            });
        };

        function send(url, booking, successMessage) {
            var action = { bookingKey: booking.bookingKey, actor: 'Walker', actorId: $scope.walkerId };

            $scope.working = true;
            apiService.post(url, action, function () {
                $scope.working = false;
                notificationService.displaySuccess(successMessage);
                loadBookings();
            }, function (error) {
                $scope.working = false;
                notificationService.displayError(error.data && error.data[0] ? error.data[0] : 'No se pudo completar la acción.');
                loadBookings();
            });
        }
    }

})(angular.module('walkyDoggy'));

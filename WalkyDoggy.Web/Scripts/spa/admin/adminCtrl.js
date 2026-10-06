(function (app) {
    'use strict';

    app.controller('adminCtrl', adminCtrl);

    adminCtrl.$inject = ['$scope', '$rootScope', '$location', '$timeout', 'servicioApi', 'servicioNotificaciones', 'servicioConfirmacion'];

    function adminCtrl($scope, $rootScope, $location, $timeout, servicioApi, servicioNotificaciones, servicioConfirmacion) {
        $scope.userData.mostrarDatosUsuario();

        //La pantalla es solo para administradores (el servidor tambien lo exige en cada funcion)
        if (!$rootScope.repository.loggedUser || $rootScope.repository.loggedUser.roleId != '1') {
            $location.path('/');
            return;
        }

        var tamanoPagina = 15;
        var tamanoBitacora = 20;

        //Se abre en el tablero: lo primero que quiere ver un administrador es como viene el sistema
        $scope.pestana = 'tablero';

        $scope.cambiarPestana = function (pestana) {
            $scope.pestana = pestana;
            if (pestana === 'administradores') {
                cargarAdministradores();
            } else if (pestana === 'bitacora') {
                cargarBitacora(1);
            }
        };

        function mostrarError(error, mensajePorDefecto) {
            servicioNotificaciones.mostrarError(error && error.data && error.data[0] ? error.data[0] : mensajePorDefecto);
        }

        /* ---------- Usuarios ---------- */

        $scope.filtros = { rol: '', estado: '', texto: '' };
        $scope.usuarios = [];
        $scope.total = 0;
        $scope.pagina = 1;
        $scope.cargando = false;

        function cargarUsuarios(pagina) {
            $scope.cargando = true;
            var parametros = { page: pagina, pageSize: tamanoPagina };
            if ($scope.filtros.rol) { parametros.role = $scope.filtros.rol; }
            if ($scope.filtros.estado) { parametros.status = $scope.filtros.estado; }
            if ($scope.filtros.texto) { parametros.search = $scope.filtros.texto; }

            servicioApi.get('/api/admin/users', { params: parametros }, function (resultado) {
                $scope.usuarios = resultado.data.users;
                $scope.total = resultado.data.total;
                $scope.pagina = resultado.data.page;
                $scope.cargando = false;
            }, function (error) {
                $scope.cargando = false;
                mostrarError(error, 'No se pudieron cargar los usuarios.');
            });
        }

        $scope.buscar = function () { cargarUsuarios(1); };

        //Mientras se escribe se busca solo, esperando un momento a que termine de tipear
        var espera = null;
        $scope.alEscribir = function () {
            $timeout.cancel(espera);
            espera = $timeout(function () { cargarUsuarios(1); }, 350);
        };
        $scope.$on('$destroy', function () { $timeout.cancel(espera); });

        $scope.hayAnterior = function () { return $scope.pagina > 1; };
        $scope.haySiguiente = function () { return $scope.pagina * tamanoPagina < $scope.total; };
        $scope.irA = function (pagina) { cargarUsuarios(pagina); };

        $scope.textoRango = function () {
            if ($scope.total === 0) {
                return '';
            }
            var desde = ($scope.pagina - 1) * tamanoPagina + 1;
            return desde + '–' + Math.min($scope.pagina * tamanoPagina, $scope.total) + ' de ' + $scope.total;
        };

        $scope.textoRol = function (usuario) {
            return usuario.role === 'Walker' ? 'Paseador' : 'Cliente';
        };

        $scope.bloquear = function (usuario) {
            var aviso = usuario.upcomingBookings > 0
                ? 'Se van a cancelar ' + usuario.upcomingBookings + (usuario.upcomingBookings === 1 ? ' reserva' : ' reservas') + ' que todavía no empezaron y se va a avisar por mail a la otra parte. '
                : 'No tiene reservas por empezar. ';

            servicioConfirmacion.preguntar({
                title: '¿Bloquear a ' + usuario.fullName + '?',
                text: aviso + 'No va a poder iniciar sesión hasta que lo desbloquees. El motivo se le muestra a la persona.',
                input: { label: 'Motivo del bloqueo', placeholder: 'Por ejemplo: maltrato a una mascota denunciado el 05/10' },
                confirmLabel: 'Bloquear',
                cancelLabel: 'Volver',
                danger: true
            }).then(function (motivo) {
                servicioApi.post('/api/admin/users/block', { userId: usuario.userId, reason: motivo }, function () {
                    servicioNotificaciones.mostrarExito('Bloqueaste la cuenta de ' + usuario.fullName + '.');
                    cargarUsuarios($scope.pagina);
                }, function (error) {
                    mostrarError(error, 'No se pudo bloquear la cuenta.');
                });
            });
        };

        $scope.desbloquear = function (usuario) {
            servicioConfirmacion.preguntar({
                title: '¿Desbloquear a ' + usuario.fullName + '?',
                text: 'Va a poder volver a iniciar sesión. Las reservas que se cancelaron al bloquearla no se recuperan.',
                confirmLabel: 'Desbloquear',
                cancelLabel: 'Volver'
            }).then(function () {
                servicioApi.post('/api/admin/users/unblock', { userId: usuario.userId }, function () {
                    servicioNotificaciones.mostrarExito('Desbloqueaste la cuenta de ' + usuario.fullName + '.');
                    cargarUsuarios($scope.pagina);
                }, function (error) {
                    mostrarError(error, 'No se pudo desbloquear la cuenta.');
                });
            });
        };

        /* ---------- Administradores ---------- */

        $scope.administradores = [];
        $scope.nuevoAdmin = { email: '', password: '', repetida: '' };
        $scope.errorAlta = '';
        $scope.guardandoAlta = false;

        function cargarAdministradores() {
            servicioApi.get('/api/admin/admins', null, function (resultado) {
                $scope.administradores = resultado.data;
            }, function (error) {
                mostrarError(error, 'No se pudieron cargar los administradores.');
            });
        }

        $scope.crearAdministrador = function () {
            var datos = $scope.nuevoAdmin;
            $scope.errorAlta = '';

            if (!datos.email || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(datos.email)) {
                $scope.errorAlta = 'Ingresá un mail válido.';
            } else if (!datos.password || datos.password.length < 6 || datos.password.length > 50) {
                $scope.errorAlta = 'La contraseña debe tener entre 6 y 50 caracteres.';
            } else if (datos.password !== datos.repetida) {
                $scope.errorAlta = 'Las contraseñas no coinciden.';
            }
            if ($scope.errorAlta) {
                return;
            }

            $scope.guardandoAlta = true;
            servicioApi.post('/api/admin/admins', { email: datos.email.trim(), password: datos.password }, function () {
                $scope.guardandoAlta = false;
                $scope.nuevoAdmin = { email: '', password: '', repetida: '' };
                servicioNotificaciones.mostrarExito('Creaste el administrador.');
                cargarAdministradores();
            }, function (error) {
                $scope.guardandoAlta = false;
                $scope.errorAlta = error.data && error.data[0] ? error.data[0] : 'No se pudo crear el administrador.';
            });
        };

        /* ---------- Bitacora ---------- */

        var textosAccion = {
            BlockUser: 'Bloqueó una cuenta',
            UnblockUser: 'Desbloqueó una cuenta',
            CreateAdmin: 'Creó un administrador',
            CancelWalk: 'Canceló una reserva',
            SaveAdvertiser: 'Publicidad',
            SetAdvertiserActive: 'Publicidad',
            SaveAd: 'Publicidad',
            SetAdActive: 'Publicidad',
            SetAdImage: 'Publicidad',
            ReviewComplaint: 'Denuncia',
            ResolveComplaint: 'Denuncia',
            ViewComplaintChat: 'Leyó un chat',
            HideRating: 'Ocultó una valoración',
            ShowRating: 'Mostró una valoración',
            HidePetReview: 'Ocultó una reseña de mascota',
            ShowPetReview: 'Mostró una reseña de mascota',
            SaveBreed: 'Raza',
            SetBreedActive: 'Raza',
            SaveSize: 'Tamaño',
            SetSizeActive: 'Tamaño',
            SaveTraitPair: 'Características',
            SetTraitPairActive: 'Características'
        };

        $scope.acciones = [];
        $scope.totalAcciones = 0;
        $scope.paginaBitacora = 1;

        $scope.textoAccion = function (accion) {
            return textosAccion[accion.action] || accion.action;
        };

        function cargarBitacora(pagina) {
            servicioApi.get('/api/admin/log', { params: { page: pagina, pageSize: tamanoBitacora } }, function (resultado) {
                $scope.acciones = resultado.data.actions;
                $scope.totalAcciones = resultado.data.total;
                $scope.paginaBitacora = resultado.data.page;
            }, function (error) {
                mostrarError(error, 'No se pudo cargar la bitácora.');
            });
        }

        $scope.hayAnteriorBitacora = function () { return $scope.paginaBitacora > 1; };
        $scope.haySiguienteBitacora = function () { return $scope.paginaBitacora * tamanoBitacora < $scope.totalAcciones; };
        $scope.irABitacora = function (pagina) { cargarBitacora(pagina); };

        cargarUsuarios(1);
    }

})(angular.module('walkyDoggy'));

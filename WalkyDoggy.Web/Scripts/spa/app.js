(function () {
    'use strict';

    angular.module('walkyDoggy', ['common.core', 'common.ui'])
        .config(config)
        .run(run);

    config.$inject = ['$routeProvider'];
    function config($routeProvider) {
        $routeProvider
            .when("/", {
                templateUrl: "scripts/spa/home/index.html",
                controller: "inicioCtrl",
                resolve: { isAuthenticated: estaAutenticado }
            })
            .when("/login", {
                templateUrl: "scripts/spa/account/login.html",
                controller: "loginCtrl"
            })
             .when("/register/customer", {
                 templateUrl: "scripts/spa/register/register-customer.html",
                 controller: "registroClienteCtrl"
             })
            .when("/register/walker", {
                templateUrl: "scripts/spa/register/register-walker.html",
                controller: "registroPaseadorCtrl"
            })
             .when("/forgot-password", {
                 templateUrl: "scripts/spa/account/forgot-password.html",
                 controller: "recuperarContrasenaCtrl"
             })
             .when("/public", {
                 templateUrl: "scripts/spa/public/homePublic.html",
                 controller: "inicioPublicoCtrl"
             })
         .when("/profile", {
             templateUrl: "scripts/spa/profile/profile.html",
             controller: "perfilCtrl",
             resolve: { isAuthenticated: estaAutenticado }
         })
        .when("/pets/list", {
            templateUrl: "scripts/spa/pets/petsList.html",
            controller: "listaMascotasCtrl",
            resolve: { isAuthenticated: estaAutenticado }
        })
        .when("/pets/edit/:id", {
            templateUrl: "scripts/spa/pets/petsEdit.html",
            controller: "editarMascotaCtrl",
            resolve: { isAuthenticated: estaAutenticado }
        })
      .when("/walks/step-1", {
          templateUrl: "scripts/spa/walks/step1.html",
          controller: "paso1Ctrl",
          resolve: { isAuthenticated: estaAutenticado }
      })
      .when("/walks/requested", {
          templateUrl: "scripts/spa/walks/requested.html",
          controller: "paseosSolicitadosCtrl",
          resolve: { isAuthenticated: estaAutenticado }
      })
      .when("/walks/step-2", {
          templateUrl: "scripts/spa/walks/step2.html",
          controller: "paso2Ctrl",
          resolve: { isAuthenticated: estaAutenticado }
      })
        .when("/walker-welcome", {
            templateUrl: "scripts/spa/register/walker-welcome.html",
            controller: "bienvenidaPaseadorCtrl",
            resolve: { isAuthenticated: estaAutenticado }
        })
        .when("/work-conditions", {
            templateUrl: "scripts/spa/workConditions/workConditions.html",
            controller: "condicionesLaboralesCtrl",
            resolve: { isAuthenticated: estaAutenticado }
        })
        .when("/walkers/:id", {
            templateUrl: "scripts/spa/walkers/walkerProfile.html",
            controller: "perfilPaseadorCtrl",
            resolve: { isAuthenticated: estaAutenticado }
        })
               .when("/error/404", {
                   templateUrl: "scripts/spa/errors/page404.html",
                   resolve: { isAuthenticated: estaAutenticado }
               })
        .otherwise({ redirectTo: "/error/404" });
    }

    run.$inject = ['$rootScope', '$location', '$cookieStore', '$http'];

    function run($rootScope, $location, $cookieStore, $http) {
        // handle page refreshes
        $rootScope.repository = $cookieStore.get('repository') || {};
        if ($rootScope.repository.loggedUser) {
            $http.defaults.headers.common['Authorization'] = $rootScope.repository.loggedUser.authdata;
        }

        $(document).ready(function () {
            $(".fancybox").fancybox({
                openEffect: 'none',
                closeEffect: 'none'
            });

            $('.fancybox-media').fancybox({
                openEffect: 'none',
                closeEffect: 'none',
                helpers: {
                    media: {}
                }
            });

            $('[data-toggle=offcanvas]').click(function () {
                $('.row-offcanvas').toggleClass('active');
            });
        });
    }

    estaAutenticado.$inject = ['servicioMembresia', '$rootScope', '$location'];

    function estaAutenticado(servicioMembresia, $rootScope, $location) {
        if (!servicioMembresia.haySesion()) {
            $rootScope.previousState = $location.path();
            $location.path('/public');
        }
    }

})();

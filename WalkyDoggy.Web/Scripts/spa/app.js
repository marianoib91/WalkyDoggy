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
                controller: "indexCtrl",
                resolve: { isAuthenticated: isAuthenticated }
            })
            .when("/login", {
                templateUrl: "scripts/spa/account/login.html",
                controller: "loginCtrl"
            })
             .when("/register/customer", {
                 templateUrl: "scripts/spa/register/register-customer.html",
                 controller: "registerCustomerCtrl"
             })
            .when("/register/walker", {
                templateUrl: "scripts/spa/register/register-walker.html",
                controller: "registerWalkerCtrl"
            })
             .when("/forgot-password", {
                 templateUrl: "scripts/spa/account/forgot-password.html",
                 controller: "forgotPasswordCtrl"
             })
             .when("/public", {
                 templateUrl: "scripts/spa/public/homePublic.html",
                 controller: "homePublicCtrl"
             })
         .when("/profile", {
             templateUrl: "scripts/spa/profile/profile.html",
             controller: "profileCtrl",
             resolve: { isAuthenticated: isAuthenticated }
         })
        .when("/pets/list", {
            templateUrl: "scripts/spa/pets/petsList.html",
            controller: "petsListCtrl",
            resolve: { isAuthenticated: isAuthenticated }
        })
        .when("/pets/edit/:id", {
            templateUrl: "scripts/spa/pets/petsEdit.html",
            controller: "petsEditCtrl",
            resolve: { isAuthenticated: isAuthenticated }
        })
      .when("/walks/step-1", {
          templateUrl: "scripts/spa/walks/step1.html",
          controller: "step1Ctrl",
          resolve: { isAuthenticated: isAuthenticated }
      })
      .when("/walks/step-2", {
          templateUrl: "scripts/spa/walks/step2.html",
          controller: "step2Ctrl",
          resolve: { isAuthenticated: isAuthenticated }
      })
        .when("/work-conditions", {
            templateUrl: "scripts/spa/workConditions/workConditions.html",
            controller: "workConditionsCtrl",
            resolve: { isAuthenticated: isAuthenticated }
        })
         .when("/work-days/edit/:id", {
             templateUrl: "scripts/spa/workDays/workDay.html",
             controller: "workDayCtrl",
             resolve: { isAuthenticated: isAuthenticated }
         })
               .when("/error/404", {
                   templateUrl: "scripts/spa/errors/page404.html",
                   resolve: { isAuthenticated: isAuthenticated }
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

    isAuthenticated.$inject = ['membershipService', '$rootScope', '$location'];

    function isAuthenticated(membershipService, $rootScope, $location) {
        if (!membershipService.isUserLoggedIn()) {
            $rootScope.previousState = $location.path();
            $location.path('/public');
        }
    }

})();
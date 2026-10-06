using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace WalkyDoggy.Web.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/Vendors/modernizr.js"));

            bundles.Add(new ScriptBundle("~/bundles/vendors").Include(
                "~/Scripts/Vendors/jquery.js",
                "~/Scripts/Vendors/bootstrap.js",
                "~/Scripts/Vendors/toastr.js",
                "~/Scripts/Vendors/jquery.raty.js",
                "~/Scripts/Vendors/respond.src.js",
                "~/Scripts/Vendors/angular.js",
                "~/Scripts/Vendors/angular-route.js",
                "~/Scripts/Vendors/angular-cookies.js",
                "~/Scripts/Vendors/angular-validator.js",
                "~/Scripts/Vendors/angular-base64.js",
                "~/Scripts/Vendors/angular-file-upload.js",
                "~/Scripts/Vendors/angucomplete-alt.min.js",
                "~/Scripts/Vendors/ui-bootstrap-tpls-0.13.1.js",
                "~/Scripts/Vendors/underscore.js",
                "~/Scripts/Vendors/raphael.js",
                "~/Scripts/Vendors/morris.js",
                "~/Scripts/Vendors/jquery.fancybox.js",
                "~/Scripts/Vendors/jquery.fancybox-media.js",
                "~/Scripts/Vendors/loading-bar.js",
                 "~/Scripts/Vendors/moment.js",
                "~/Scripts/Vendors/daterangepicker.js",
                "~/Scripts/Vendors/es6-promise.min.js",
                "~/Scripts/Vendors/sweetalert.min.js"

                ));

            bundles.Add(new ScriptBundle("~/bundles/spa").Include(
                "~/Scripts/spa/modules/common.core.js",
                "~/Scripts/spa/modules/common.ui.js",
                "~/Scripts/spa/app.js",
                "~/Scripts/spa/services/apiService.js",
                "~/Scripts/spa/services/notificationService.js",
                "~/Scripts/spa/services/membershipService.js",
                "~/Scripts/spa/services/fileUploadService.js",
                "~/Scripts/spa/services/geocodingService.js",
                "~/Scripts/spa/services/confirmService.js",
                "~/Scripts/spa/services/chatService.js",
                "~/Scripts/spa/services/complaintService.js",
                "~/Scripts/spa/services/advertisingService.js",
                "~/Scripts/spa/services/favoritesService.js",
                "~/Scripts/spa/services/traitsService.js",
                "~/Scripts/spa/layout/topBar.directive.js",
                "~/Scripts/spa/directives/stars.directive.js",
                "~/Scripts/spa/filters/money.filter.js",
                "~/Scripts/spa/directives/angular-block-ui.min.js",
                "~/Scripts/spa/directives/angular-daterangepicker.js",
                "~/Scripts/spa/directives/addressField.directive.js",
                "~/Scripts/spa/directives/sweetalert.js",
                 //  "~/Scripts/spa/account/loginCtrl.js",
                 "~/Scripts/spa/account/loginModalCtrl.js",
                "~/Scripts/spa/account/changePasswordModalCtrl.js",
                "~/Scripts/spa/admin/adminCtrl.js",
                "~/Scripts/spa/friends/friendsCtrl.js",
                "~/Scripts/spa/stays/staysCtrl.js",
                "~/Scripts/spa/stays/stayCardCtrl.js",
                "~/Scripts/spa/admin/adminCatalogosCtrl.js",
                "~/Scripts/spa/admin/adminDenunciasCtrl.js",
                "~/Scripts/spa/admin/adminResenasCtrl.js",
                "~/Scripts/spa/admin/adminTableroCtrl.js",
                "~/Scripts/spa/admin/adminPublicidadCtrl.js",
                "~/Scripts/spa/directives/publicidad.directive.js",
                "~/Scripts/spa/admin/adminPaseosCtrl.js",
                "~/Scripts/spa/complaints/complaintModalCtrl.js",
                "~/Scripts/spa/register/registerModalCtrl.js",
                "~/Scripts/spa/register/registerWalkerCtrl.js",
                "~/Scripts/spa/register/walkerWelcomeCtrl.js",
                "~/Scripts/spa/chat/chatModalCtrl.js",
                "~/Scripts/spa/register/registerCustomerCtrl.js",
                "~/Scripts/spa/account/forgotPasswordCtrl.js",
                "~/Scripts/spa/home/rootCtrl.js",
                "~/Scripts/spa/home/indexCtrl.js",
                "~/Scripts/spa/home/buscarPaseadoresCtrl.js",
                "~/Scripts/spa/public/homePublicCtrl.js",
                "~/Scripts/spa/profile/profileCtrl.js",
                "~/Scripts/spa/pets/petsListCtrl.js",
                "~/Scripts/spa/pets/petsEditCtrl.js",
                 "~/Scripts/spa/walks/step1Ctrl.js",
                 "~/Scripts/spa/walks/step2Ctrl.js",
                 "~/Scripts/spa/walks/requestedCtrl.js",
                  "~/Scripts/spa/workConditions/workConditionsCtrl.js",
                  "~/Scripts/spa/walkers/walkerProfileCtrl.js"
                ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/content/css/custom.css",
                "~/content/css/site.css",
                "~/content/css/bootstrap.css",
                 "~/content/css/font-awesome.css",
                "~/content/css/morris.css",
                "~/content/css/toastr.css",
                "~/content/css/jquery.fancybox.css",
                "~/content/css/loading-bar.css",
                "~/content/css/vendors/blockui/angular-block-ui.min.css",
                "~/content/css/vendors/daterangepicker/daterangepicker.css",
                "~/content/css/vendors/sweetalert/sweetalert.css",
                "~/content/css/walky-theme.css"));


            BundleTable.EnableOptimizations = false;
        }
    }
}
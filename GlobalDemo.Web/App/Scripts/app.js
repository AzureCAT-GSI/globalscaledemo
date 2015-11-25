'use strict';
angular.module('todoApp', ['ngRoute','AdalAngular','azureBlobUpload'])
.config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', function ($routeProvider, $httpProvider, adalProvider) {

    $routeProvider.when("/Home", {
        controller: "homeCtrl",
        templateUrl: "/App/Views/Home.html",
    }).when("/MyPhotos", {
        controller: "myPhotosCtrl",
        templateUrl: "/App/Views/MyPhotos.html",
        requireADLogin: true,
    }).when("/Upload", {
        controller: "uploadCtrl",
        templateUrl: "/App/Views/Upload.html",
        requireADLogin: true,
    }).when("/UserData", {
        controller: "userDataCtrl",
        templateUrl: "/App/Views/UserData.html",
    }).otherwise({ redirectTo: "/Home" });

    adalProvider.init(
        {
            instance: '', 
            tenant: '',
            clientId: '',
            extraQueryParameter: 'nux=1',
            cacheLocation: 'localStorage', // enable this for IE, as sessionStorage does not work for localhost.
        },
        $httpProvider
        );
   
}]);

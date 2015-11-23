'use strict';
angular.module('todoApp')
.controller('homeCtrl', ['$scope', 'adalAuthenticationService','$location', 'homeSvc', function ($scope, adalService, $location, homeSvc) {
    $scope.photoList = null;
    $scope.loadingMessage = "Loading...";
    $scope.error = "";
    $scope.startTime = "";
    $scope.elapsedTime = "";

    $scope.login = function () {
        adalService.login();
    };
    $scope.logout = function () {
        adalService.logOut();
    };
    $scope.isActive = function (viewLocation) {        
        return viewLocation === $location.path();
    };

    $scope.populate = function () {
        $scope.startTime = new Date().getTime();
        homeSvc.getItems().success(function (results) {
            $scope.photoList = results;
            $scope.elapsedTime = new Date().getTime() - $scope.startTime;
            $scope.loadingMessage = "Found " + $scope.photoList.length + " items in " + $scope.elapsedTime + " ms";
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
}]);
'use strict';
angular.module('todoApp')
.controller('myPhotosCtrl', ['$scope', '$location', 'myPhotosSvc', 'adalAuthenticationService', function ($scope, $location, myPhotosSvc, adalService) {
    $scope.photoList = null;
    $scope.loadingMessage = "Loading...";
    $scope.error = "";
    $scope.startTime = "";
    $scope.elapsedTime = "";

    $scope.populate = function () {
        $scope.loadingMessage = "Loading...";
        $scope.startTime = new Date().getTime();
        myPhotosSvc.getItems().success(function (results) {
            $scope.photoList = results;
            $scope.elapsedTime = new Date().getTime() - $scope.startTime;
            $scope.loadingMessage = "Loaded " + results.length + " items in " + $scope.elapsedTime + " ms"; 
        }).error(function (err) { 
                $scope.error = err; 
                $scope.loadingMessage = ""; 
        }) 
    };

    $scope.clearUserCache = function () {
        $scope.loadingMessage = "Clearing user cache...";
        myPhotosSvc.deleteUserCache().success(function () {
            $scope.loadingMessage = "";
            $scope.photoList = null;
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
    
    $scope.clearAllCache = function () {
        $scope.loadingMessage = "Clearing all caches...";
        myPhotosSvc.deleteAll().success(function () {
            $scope.loadingMessage = "";
            $scope.photoList = null;
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
}]);

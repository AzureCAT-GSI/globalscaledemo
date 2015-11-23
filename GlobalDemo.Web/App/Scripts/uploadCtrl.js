'use strict';
angular.module('todoApp')
.controller('uploadCtrl', ['$scope', '$location', 'azureBlob', 'uploadSvc', 'adalAuthenticationService', function ($scope, $location, azureBlob, uploadSvc, adalService) {
    $scope.error = "";
    $scope.sasToken = "";
    $scope.config = null;
    $scope.uploadComplete = false;
    $scope.progress = 0;
    
    $scope.cancellationToken = null;
    
    $scope.fileChanged = function () {
        console.log($scope.file.name);
    };

    $scope.upload = function () {

        var myFileTemp = document.getElementById("myFile");
        console.log("File name: " + myFileTemp.files[0].name);
        var extension = myFileTemp.files[0].name.split('.').pop();

        uploadSvc.getSASToken(extension).success(function (results) {
                      
            console.log("SASToken: " + results.BlobSASToken);
            
            $scope.config =
            {
                baseUrl: results.BlobURL,
                sasToken: results.BlobSASToken,
                file: myFileTemp.files[0],
                blockSize: 1024 * 32,

                progress: function (amount) {                    
                    console.log("Progress - " + amount);
                    $scope.progress = amount;
                    console.log(amount);
                },
                complete: function () {
                    console.log("Completed!");
                    $scope.progress = 99.99;
                    uploadSvc.postItem(
                        {
                            'ID' : results.ID,
                            'ServerFileName': results.ServerFileName,
                            'StorageAccountName': results.StorageAccountName,
                            'BlobURL': results.BlobURL
                        }).success(function () {
                        $scope.uploadComplete = true;
                    }).error(function (err) {
                        console.log("Error - " + err);
                        $scope.error = err;
                        $scope.uploadComplete = false;
                    });                    
                },
                error: function (data, status, err, config) {
                    console.log("Error - " + data);
                    $scope.error = data;
                }
            };
            azureBlob.upload($scope.config);

        }).error(function (err) {            
            $scope.error = err;
        });
    };

}])
.directive('fileInput'[function ($parse) {
    return {
        restrict: "EA",
        template: "<input type='file' />",
        replace: true,
        link: function (scope, element, attrs) {

            var modelGet = $parse(attrs.fileInput);
            var modelSet = modelGet.assign;
            var onChange = $parse(attrs.onChange);

            var updateModel = function () {
                scope.$apply(function () {
                    modelSet(scope, element[0].files[0]);
                    onChange(scope);
                });
            };

            element.bind('change', updateModel);
        }
    };
}]);
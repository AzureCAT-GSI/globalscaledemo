'use strict';
angular.module('todoApp')
.factory('uploadSvc', ['$http', function ($http) {
    return {
        getSASToken: function (extension) {
            return $http.get('/api/Upload/' + extension);
        },
        postItem: function (item) {
            return $http.post('/api/Upload/', item);
        }
    };
}]);
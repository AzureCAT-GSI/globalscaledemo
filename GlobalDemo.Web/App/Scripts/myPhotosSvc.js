'use strict';
angular.module('todoApp')
.factory('myPhotosSvc', ['$http', function ($http) {
    return {
        getItems: function(){
            return $http.get('/api/UserPhotos');
        },
        getItem: function (id) {
            return $http.get('/api/UserPhotos/' + id);
        },
        deleteAll: function () {
            return $http({
                method: 'DELETE',
                url: '/api/UserPhotos'
            });
        },
        deleteUserCache: function () {
            return $http({
                method: 'DELETE',
                url: '/api/UserPhotos/1'
            });
        }
    };
}]);
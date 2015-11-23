'use strict';
angular.module('todoApp')
.factory('homeSvc', ['$http', function ($http) {
    return {
        getItems: function () {
            return $http.get('/api/Photo');
        },
        getItem: function (id) {
            return $http.get('/api/Photo/' + id);
        }
    };
}]);
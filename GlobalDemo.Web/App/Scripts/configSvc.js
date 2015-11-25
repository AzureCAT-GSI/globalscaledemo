'use strict';
angular.module('todoApp')
.factory('configSvc', ['$http', function ($http) {
    return {
        getConfig: function () {
            console.log('w00t!');
            return $http.get('/api/adalconfig');
        }
    };
}]);
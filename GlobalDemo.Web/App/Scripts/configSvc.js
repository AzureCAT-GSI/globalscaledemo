'use strict';
angular.module('todoApp')
.factory('configSvc', ['$http', function ($http) {
    return {
        getConfig: function () {            
            return $http.get('/api/adalconfig');
        }
    };
}]);
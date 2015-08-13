(function () {
    'use strict';

    angular
        .module('azureapp')
        .factory('factory', factory);

    factory.$inject = ['$http'];

    function factory($http) {
        var service = {
            getData: getData
        };

        return service;

        function getData() {
            return $http.get('http://localhost/AutenticacionAzure/Servicios/api/datos');
        }
    }
})();
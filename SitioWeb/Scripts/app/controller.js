(function () {
    'use strict';

    angular
        .module('azureapp')
        .controller('controller', controller);

    controller.$inject = ['$location', 'factory', 'adalAuthenticationService'];

    function controller($location, factory,adalAuthenticationService)  {
        /* jshint validthis:true */
        var vm = this;
        vm.title = 'controller';

        vm.logout = function () {
            adalAuthenticationService.logOut();
        };

        vm.login = function () {
            adalAuthenticationService.login();
        };


        vm.Consultar = function () {
            console.log('consultar');

            factory.getData().then(
                function (data) {
                    vm.Personas = data.data;
                    console.log(vm.Personas);
                },
                function (error) {
                    vm.Error = error;
                    //alert(error.data.Message);
                }
                );
        };
    }
})();

(function () {
    'use strict';

    //angular.module('azureapp').config(function ($stateProvider, $urlRouterProvider, $httpProvider, adalProvider) {
    angular.module('azureapp')
.config(['$stateProvider', '$urlRouterProvider','$httpProvider', 'adalAuthenticationServiceProvider',
 function ($stateProvider, $urlRouterProvider, $httpProvider, adalProvider){

        //
        // For any unmatched url, redirect to /state1
        $urlRouterProvider.otherwise("/pag1");
        //
        // Now set up the states
        $stateProvider
          .state('pag1', {
              url: "/pag1",
              templateUrl: "Views/Pagina1.html"
          })      
          .state('pag2', {
              url: "/pag2",
              templateUrl: "Views/Pagina2.html",
              requireADLogin: false
          })
        .state('pag3', {
            url: "/pag3",
            templateUrl: "Views/Pagina3.html",
            requireADLogin: true
        });

        adalProvider.init(
        {
            instance: 'https://login.microsoftonline.com/',
            tenant: 'desarrolloarkix.onmicrosoft.com',
            clientId: '92c60ccd-3fd1-48f6-a2c0-4f7f63a31668',
            extraQueryParameter: 'nux=1',
            //cacheLocation: 'localStorage', // enable this for IE, as sessionStorage does not work for localhost.
        },
        $httpProvider
        );


    }]);


})();

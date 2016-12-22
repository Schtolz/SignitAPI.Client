var integrationApp = angular.module('integrationApp', [
	'ngRoute',
    'ngFileUpload'
]);

integrationApp.config(function ($routeProvider) {
    $routeProvider

        // route for the home page
        .when('/', {
            templateUrl: 'templates/indexTemplate.html',
            controller: 'indexController'
        })

        // route for the about page
        .when('/details/:orderId', {
            templateUrl: 'templates/detailsTemplate.html',
            controller: 'detailsController'
        })

        // route for the contact page
        .when('/sign/:orderId/:localSignerReference', {
            templateUrl: 'templates/signTemplate.html',
            controller: 'signController'
        });
});
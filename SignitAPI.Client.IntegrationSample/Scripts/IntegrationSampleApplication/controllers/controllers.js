integrationApp.controller('indexController', ['$scope', '$http', '$location', 'orderService', 'APP_DATA', function ($scope, $http, $location, orderService, APP_DATA) {
    $scope.formData = { file: [], convert: true, addLabel: true };
    
    // create a message to display in our view
    $scope.message = 'Everyone come and see how good I look!';

    $scope.postOrder = function () {
        $scope.formData.file = $scope.file;
        $scope.formData.message = $scope.message;
        $scope.formData.signers = angular.toJson([{ LocalSignerReference: APP_DATA.LOCAL_SIGNER_REFERENCE, Name: APP_DATA.LOCAL_SIGNER_NAME, Email: APP_DATA.LOCAL_SIGNER_EMAIL, SendSignitEmails: true }]);
        orderService.postOrder($scope.formData).then(function (data) {
            $location.path("/details/" + data);
        }, function (msg) {
            $scope.error = msg;
        });
    }
}]);

integrationApp.controller('detailsController', ['$scope', '$http', '$routeParams', 'orderService', '$location', 'APP_DATA', function ($scope, $http, $routeParams, orderService, $location, APP_DATA) {

    // create a message to display in our view
    $scope.routeParams = $routeParams;
    $scope.message = 'Order details';
    $scope.localSignerReference = APP_DATA.LOCAL_SIGNER_REFERENCE;
    $scope.order = {};

    $scope.init = function(orderId) {
        $scope.getOrderDetails(orderId);
    }

    $scope.getOrderDetails = function (orderId) {
        orderService.getOrderDetails(orderId).then(function (data) {
            $scope.order = data.OrderDetails;
        }, function (msg) {
            $scope.error = msg.Content;
        });
    }

    $scope.gotoSign = function(orderId) {
        $location.path("/sign/" + orderId + "/" + APP_DATA.LOCAL_SIGNER_REFERENCE);
    }

    $scope.init($scope.routeParams.orderId);
}]);

integrationApp.controller('signController', ['$scope', '$http', '$routeParams', 'orderService', '$sce', 'APP_DATA', function ($scope, $http, $routeParams, orderService, $sce, APP_DATA) {
    var ctrl = this;
    $scope.routeParams = $routeParams;
    // create a message to display in our view
    $scope.message = 'Sign here';
    $scope.sref = '';
    $scope.initialized = false;
    

    $scope.init = function () {
        orderService.getSignDetails($scope.routeParams.orderId, $scope.routeParams.localSignerReference).then(function (data) {
            $scope.sref = data.Sref;
            $scope.initialized = true;
            $scope.signUrl = $sce.trustAsResourceUrl($scope.getSigningUrl());
        }, function (msg) {
            $scope.error = msg;
        });
    }

    $scope.getSigningUrl = function () {
        var css = ".eSigning{width: 500px; height:500px; border:0px;}";
        return APP_DATA.SIGNIT_BASE_URL+'/document/sign?sref=' + $scope.sref+'&css='+css;
    }

    $scope.init();
}]);
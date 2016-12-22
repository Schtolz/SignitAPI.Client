integrationApp.service('orderService', ['$http', '$q', 'Upload', 'APP_DATA', function ($http, $q, Upload, APP_DATA) {
        this.baseUrl = '/api/v1';

        this.postOrder = function(formData) {
            var defer = $q.defer();

            Upload.upload({
                url: this.baseUrl+'/order',
                method: 'POST',
                forceIFrameUpload: false,
                data: formData,
                headers: {
                    '__RequestVerificationToken': $(':input:hidden[name*="RequestVerificationToken"]').val()
                }
            }).then(
                function(response) {
                    defer.resolve(response.data);
                    //console.log("success ", response.data);
                },
                function(response) {
                    defer.reject(response.data);
                    //console.log(response);
                }
            );

            return defer.promise;
        }

        this.getOrderDetails = function(orderId) {
            var defer = $q.defer();
            $http.get(this.baseUrl + '/details/' + orderId).then(function (response) {
                    defer.resolve(response.data);
                },
                function(response) {
                    defer.reject(response.data);
                });
            return defer.promise;
        }

        this.getSignDetails = function(orderId, localSignerReference) {
            var defer = $q.defer();
            $http.get(this.baseUrl+'/sign?localSignerReference=' + localSignerReference + '&orderId=' + orderId + '&localDocumentReference=&successRedirectPage=1').then(function(response) {
                    defer.resolve(response.data);
                },
                function(response) {
                    defer.reject(response.data);
                });
            return defer.promise;
        }
    }
]);


integrationApp.service('multipartForm', [
	'$http', function ($http) {
	    this.post = function (uploadUrl, data, successCallback, failCallback) {
	        var fd = new FormData();
	        for (var key in data) {
	            if (Array.isArray(data[key])) {
	                for (var subkey in data[key]) {
	                    fd.append(key, data[key][subkey]);
	                }
	            }
	            fd.append(key, data[key]);
	        }
	        $http.post(uploadUrl, fd, {
	            transformRequest: angular.identity,
	            headers: {
	                'Content-type': undefined
	            }
	        }).then(successCallback, failCallback);
	    }
	}
]);


function modelzBuilderController($scope, $http, umbRequestHelper, modelzBuilderResource) {

    var vm = this;

    vm.reload = reload;
    vm.generate = generate;
    vm.dashboard = null;

    function generate() {
        vm.generating = true;
        umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl("modelzBuilderBaseUrl", "BuildModels")),
                'Failed to generate.')
            .then(function (result) {
                vm.generating = false;
                vm.dashboard = result;
            });
    }

    function reload() {
        vm.loading = true;
        modelzBuilderResource.getDashboard().then(function (result) {
            vm.dashboard = result;
            vm.loading = false;
        });
    }

    function init() {
        vm.loading = true;
        modelzBuilderResource.getDashboard().then(function (result) {
            vm.dashboard = result;
            vm.loading = false;
        });
    }

    init();
}
angular.module("umbraco").controller("ZpqrtBnk.ModelzBuilder.ModelzBuilderController", modelzBuilderController);
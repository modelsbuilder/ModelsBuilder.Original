function modelzBuilderController(modelzBuilderResource) {

    var vm = this;

    vm.reload = reload;
    vm.generate = generate;
    vm.dashboard = null;

    function generate() {
        vm.generating = true;
        modelzBuilderResource.buildModels().then(function (result) {
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
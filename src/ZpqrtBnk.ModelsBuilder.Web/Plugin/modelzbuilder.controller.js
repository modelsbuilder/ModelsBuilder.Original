function modelsBuilderController(modelsBuilderResource) {

    var vm = this;

    vm.reload = reload;
    vm.generate = generate;
    vm.dashboard = null;

    function generate() {
        vm.generating = true;
        modelsBuilderResource.buildModels().then(function (result) {
            vm.generating = false;
            vm.dashboard = result;
        });
    }

    function reload() {
        vm.loading = true;
        modelsBuilderResource.getDashboard().then(function (result) {
            vm.dashboard = result;
            vm.loading = false;
        });
    }

    function init() {
        vm.loading = true;
        modelsBuilderResource.getDashboard().then(function (result) {
            vm.dashboard = result;
            vm.loading = false;
        });
    }

    init();
}
angular.module("umbraco").controller("ZpqrtBnk.ModelsBuilder.ModelsBuilderController", modelsBuilderController);
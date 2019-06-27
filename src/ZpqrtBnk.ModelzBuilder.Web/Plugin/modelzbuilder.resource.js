function modelzBuilderResource($q, $http, umbRequestHelper) {

    return {
        getModelsOutOfDateStatus: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl("modelzBuilderBaseUrl", "GetModelsOutOfDateStatus")),
                "Failed to get models out-of-date status");
        },

        buildModels: function () {
            return umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl("modelzBuilderBaseUrl", "BuildModels")),
                "Failed to build models");
        },

        getDashboard: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl("modelzBuilderBaseUrl", "GetDashboard")),
                "Failed to get dashboard");
        }
    };
}
angular.module("umbraco.resources").factory("modelzBuilderResource", modelzBuilderResource);

// also register it as 'modelsBuilderResource' as that is required for the Core UI
// to enhance the 'Save' buttons with 'Save and Generate Models' - see edit.html views,
// edit.controller.js controllers, and the contenttypehelper.service.js service
angular.module("umbraco.resources").factory("modelsBuilderResource", modelzBuilderResource);
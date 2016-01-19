function modelsBuilderResource($q, $http, umbRequestHelper) {

    return {
        getModelsOutOfDateStatus: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl("modelsBuilderBaseUrl", "GetModelsOutOfDateStatus")),
                "Failed to get models out-of-date status");
        },

        buildModels: function () {
            return umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl("modelsBuilderBaseUrl", "BuildModels")),
                "Failed to build models");
        }
    };
}
angular.module("umbraco.resources").factory("modelsBuilderResource", modelsBuilderResource);

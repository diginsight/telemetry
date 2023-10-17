using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasySample
{
    public class TestPayload
    {
        public Guid GrupId { get; set; }
        public Dictionary<string, string> Requests { get; set; }
    }

    public interface ITestCachePreload
    {
        //[Headers("APIm KEY: sdadsadas")]
        [Headers("Testnames: PreparePreloadRulesAsync,RunEquipmentsOnBehalfOfAsync")]
        [Post("/api/v2/TestCachePreload/organization/{organizationId}/site/{siteId}/runall")] // /energy-api
        public Task<ApiResponse<string>> RunAllTests(Guid organizationId, Guid siteId, [Body] TestPayload payload); // TestPayload [Body(BodySerializationMethod.Json)]

    }
}

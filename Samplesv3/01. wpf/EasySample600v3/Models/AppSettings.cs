using System;
using System.Collections.Generic;

namespace EasySample
{
    public class AppSettings
    {
        public AutomaticTestsOptions AutomaticTests { get; set; } = new AutomaticTestsOptions();
        public CachePreloadOptions CachePreload { get; set; } = new CachePreloadOptions();
    }

    public class CachePreloadOptions
    {
        public bool Enabled { get; set; }

        public string BaseUrl { get; set; }

        public List<AutomaticTestsDetailsOption> Details { get; set; } = new List<AutomaticTestsDetailsOption>();
    }

    public class AutomaticTestsOptions
    {
        public bool Enabled { get; set; }

        public string BaseUrl { get; set; }

        public List<AutomaticTestsDetailsOption> Details { get; set; } = new List<AutomaticTestsDetailsOption>();
    }

    public class AutomaticTestsDetailsOption
    {
        public Guid OrganizationId { get; set; }
        public Guid SiteId { get; set; }
    }
   
}

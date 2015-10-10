using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public class PluginDefinition
    {
        public string Key { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string License { get; set; }
        public string OrganizationUrl { get; set; }
        public string Homepage { get; set; }
        public string Class { get; set; }
        public string SourcesUrl { get; set; }
        public string Developers { get; set; }
        public string IssueTrackerUrl { get; set; }
        public string TermsConditionsUrl { get; set; }
        public string Organization { get; set; }
    }
}

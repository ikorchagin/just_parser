using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustParserLib.Avito
{
    public class AvitoParserSettings : IParserSettings
    {
        public bool HasPages { get; set; }

        public string Prefix { get; set; } = "?p={value}";

        public string HelperPrefix { get; set; } = "&p={value}";

        public string Url { get; set; }

        public int ItemsCount { get; set; } = 0;
        public AgentType MainPageAgent { get; set; } = AgentType.Desktop;
    }
}

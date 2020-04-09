using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustParserLib
{
    public interface IParserSettings
    {
        bool HasPages { get; set; }

        string Prefix { get; set; }

        string HelperPrefix { get; set; }

        string Url { get; set; }

        int ItemsCount { get; set; }

        AgentType MainPageAgent { get; set; }
    }
}

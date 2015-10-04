using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.SonarQube
{
    public class RuleGenerator : IRuleGenerator
    {
        #region IRuleGenerator

        public string GenerateRuleXml(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            foreach(DiagnosticAnalyzer analyzer in analyzers)
            {
            }

            return null;
        }

        #endregion

        #region Private methods

        private static void ProcessAnalyzer()
        {

        }

        #endregion

    }
}

// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace SonarLint.RuleDescriptors
{
    public class SqaleRemediation
    {
        public SqaleRemediation()
        {
            Properties = new List<SqaleRemediationProperty>();
        }

        public List<SqaleRemediationProperty> Properties { get; private set; }
    }
}
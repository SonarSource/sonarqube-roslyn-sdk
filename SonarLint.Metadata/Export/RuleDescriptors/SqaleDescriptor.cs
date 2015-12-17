// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.RuleDescriptors
{
    public class SqaleDescriptor
    {
        public SqaleDescriptor()
        {
            Remediation = new SqaleRemediation();
        }

        public string SubCharacteristic { get; set; }

        public SqaleRemediation Remediation { get; set; }
    }
}
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace SonarLint.RuleDescriptors
{
    public class RuleDetail
    {
        public RuleDetail()
        {
            Tags = new List<string>();
            Parameters = new List<RuleParameter>();
            CodeFixTitles = new List<string>();
        }

        public string Key { get; set; }
        public string Title { get; set; }
        public string Severity { get; set; }
        public int IdeSeverity { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; private set; }
        public List<RuleParameter> Parameters { get; private set; }
        public bool IsActivatedByDefault { get; set; }
        public SqaleDescriptor SqaleDescriptor { get; set; }
        public List<string> CodeFixTitles { get; private set; }
    }
}
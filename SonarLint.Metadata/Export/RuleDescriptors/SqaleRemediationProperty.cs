// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.RuleDescriptors
{
    public class SqaleRemediationProperty
    {
        public const string RemediationFunctionKey = "remediationFunction";
        public const string ConstantRemediationFunctionValue = "CONSTANT_ISSUE";
        public const string OffsetKey = "offset";

        public string Key { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
    }
}
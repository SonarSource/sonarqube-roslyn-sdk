//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RuleAttribute : Attribute
    {
        public string Key { get; private set; }
        public string Title { get; private set; }
        public Severity Severity { get; private set; }
        public bool IsActivatedByDefault { get; private set; }

        public RuleAttribute(string key, Severity severity, string title, bool isActivatedByDefault)
        {
            Key = key;
            Title = title;
            Severity = severity;
            IsActivatedByDefault = isActivatedByDefault;
        }
    }
}
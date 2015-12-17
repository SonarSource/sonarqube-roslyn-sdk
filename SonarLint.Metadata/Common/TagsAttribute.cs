//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SonarLint.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TagsAttribute : Attribute
    {
        public IEnumerable<string> Tags { get; private set; }

        public TagsAttribute(params string[] tags)
        {
            Tags = tags;
        }
    }
}

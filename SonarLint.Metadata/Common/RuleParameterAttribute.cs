//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;

namespace SonarLint.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RuleParameterAttribute : Attribute
    {
        public string Key { get; private set; }
        public string Description { get; private set; }
        public PropertyType Type { get; private set; }
        public string DefaultValue { get; private set; }

        public RuleParameterAttribute(string key, PropertyType type, string description, string defaultValue)
        {
            Key = key;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
        }
        public RuleParameterAttribute(string key, PropertyType type, string description, int defaultValue)
            : this(key, type, description, defaultValue.ToString(CultureInfo.InvariantCulture))
        {
        }
        public RuleParameterAttribute(string key, PropertyType type, string description)
            : this(key, type, description, null)
        {
        }
        public RuleParameterAttribute(string key, PropertyType type)
            : this(key, type, null, null)
        {
        }
    }
}
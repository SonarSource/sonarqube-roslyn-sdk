//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.Common.Sqale
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SqaleSubCharacteristicAttribute : Attribute
    {
        public SqaleSubCharacteristic SubCharacteristic { get; private set; }

        public SqaleSubCharacteristicAttribute(SqaleSubCharacteristic subCharacteristic)
        {
            SubCharacteristic = subCharacteristic;
        }
    }
}
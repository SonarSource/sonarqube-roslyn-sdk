// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    [XmlRoot("sqale", Namespace = "")]
    public class SqaleRoot
    {
        public SqaleRoot()
        {
            Sqale = new List<SqaleDescriptor>();
        }
        [XmlArray("chc")]
        public List<SqaleDescriptor> Sqale { get; private set; }
    }
}
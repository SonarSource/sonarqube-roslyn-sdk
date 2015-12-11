//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.Utilities;
using SonarLint.XmlDescriptor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SonarLint
{
    [TestClass]
    public class DescriptorGenerator
    {
        [TestMethod]
        [TestCategory("SonarLint")]
        public void Generate_Descriptor()
        {
            WriteXmlDescriptorFiles("rules.xml", "profile.xml", "sqale.xml");
        }

        private static void WriteXmlDescriptorFiles(string rulePath, string profilePath, string sqalePath)
        {
            var genericRuleDetails = RuleDetailBuilder.GetAllRuleDetails().ToList();
            var ruleDetails = genericRuleDetails.Select(RuleDetail.Convert).ToList();
            var sqaleDetails = genericRuleDetails.Select(SqaleDescriptor.Convert).ToList();

            WriteRuleDescriptorFile(rulePath, ruleDetails);
            WriteQualityProfileFile(profilePath, ruleDetails);
            WriteSqaleDescriptorFile(sqalePath, sqaleDetails);
        }

        private static void WriteSqaleDescriptorFile(string filePath, IEnumerable<SqaleDescriptor> sqaleDescriptions)
        {
            var root = new SqaleRoot();
            root.Sqale.AddRange(sqaleDescriptions
                .Where(descriptor => descriptor != null));
            SerializeObjectToFile(filePath, root);
        }

        private static void WriteQualityProfileFile(string filePath, IEnumerable<RuleDetail> ruleDetails)
        {
            var root = new QualityProfileRoot();
            root.Rules.AddRange(ruleDetails
                .Where(descriptor => descriptor.IsActivatedByDefault)
                .Select(descriptor => new QualityProfileRuleDescriptor
                {
                    Key = descriptor.Key
                }));

            SerializeObjectToFile(filePath, root);
        }

        private static void WriteRuleDescriptorFile(string filePath, IEnumerable<RuleDetail> ruleDetails)
        {
            var root = new RuleDescriptorRoot();
            root.Rules.AddRange(ruleDetails);
            SerializeObjectToFile(filePath, root);
        }

        private static void SerializeObjectToFile(string filePath, object objectToSerialize)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                IndentChars = "  "
            };

            using (var stream = new MemoryStream())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(objectToSerialize.GetType());
                serializer.Serialize(writer, objectToSerialize, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                var ruleXml = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(filePath, ruleXml);
            }
        }
    }
}
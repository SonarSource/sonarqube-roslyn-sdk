using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
{
    [XmlRoot(ElementName = "rules")]
    public class Rules : List<Rule>
    {
        #region Serialization

        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>
        /// Saves the project to the specified file as XML
        /// </summary>
        public int Save(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            this.FileName = fileName;

            Rules rulesToSave = new Rules();

            foreach (Rule rule in this)
            {
                if (String.IsNullOrWhiteSpace(rule.Key))
                {
                    Console.WriteLine(Resources.WARN_EmptyKey);
                    continue;
                }

                if (rulesToSave.Any(r => String.Equals(r.Key, rule.Key, Rule.RuleKeyComparer)))
                {
                    Console.WriteLine(Resources.WARN_DuplicateKey, rule.Key);
                    continue;
                }

                Debug.Assert(
                    rule.Tags == null ||
                    rule.Tags.All(t => String.Equals(t, t.ToLowerInvariant(), StringComparison.Ordinal)),
                    "SonarQube tags have to be lower case ");

                rulesToSave.Add(rule);
            }

            Serializer.SaveModel(rulesToSave, fileName);

            return rulesToSave.Count;
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static Rules Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            Rules model = Serializer.LoadModel<Rules>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion Serialization
    }
}
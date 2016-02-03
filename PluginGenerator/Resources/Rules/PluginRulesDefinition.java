//-----------------------------------------------------------------------
// <copyright file="PluginRulesDefinition.java" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
package [PLUGIN_PACKAGE];

import java.io.InputStream;
import java.util.Arrays;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.api.server.rule.RulesDefinitionXmlLoader;
import org.sonar.squidbridge.rules.SqaleXmlLoader;

public final class PluginRulesDefinition implements RulesDefinition {

  private static final Logger LOG = LoggerFactory.getLogger(PluginRulesDefinition.class);

  // Unique to the plugin: used when naming the repository and the properties
  protected static final String REPOSITORY_ID = "[RULE_REPOSITORY_ID]";
  
  protected static final List<String> LANGUAGE_KEYS = Arrays.asList("[RULE_LANGUAGE]");

  public PluginRulesDefinition() {
  }

  protected String rulesDefinitionFilePath() {
    return "/resources/[RULE_RESOURCE_ID].rules.xml";
  }

  protected String sqaleDefinitionFilePath() {
    return "/resources/[RULE_RESOURCE_ID].sqale.xml";
  }

  private void defineRulesForLanguage(Context context, String repositoryKey, String repositoryName, String languageKey) {
    NewRepository repository = context.createRepository(repositoryKey, languageKey).setName(repositoryName);

    InputStream rulesXml = this.getClass().getResourceAsStream(rulesDefinitionFilePath());
    if (rulesXml == null)
	{
      LOG.info(Plugin.KEY + ": rule information was not found");
	}
    else
	{
      LOG.info(Plugin.KEY + ": loading rule information from resource...");
      RulesDefinitionXmlLoader rulesLoader = new RulesDefinitionXmlLoader();
      rulesLoader.load(repository, rulesXml, "utf8"); // Charsets.UTF_8.name());
    }

	if (this.getClass().getResourceAsStream(sqaleDefinitionFilePath()) == null)
    {
      LOG.info(Plugin.KEY + ": sqale information was not found");
    }
	else
	{
      LOG.info(Plugin.KEY + ": loading Sqale information from resource...");
	  SqaleXmlLoader.load(repository, sqaleDefinitionFilePath());
	}
	
    repository.done();
  }

  @Override
  public void define(Context context) {
    for (String languageKey : LANGUAGE_KEYS) {
      defineRulesForLanguage(context, PluginRulesDefinition.getRepositoryKeyForLanguage(languageKey), PluginRulesDefinition.getRepositoryNameForLanguage(languageKey),
              languageKey);
    }
  }

  public static String getRepositoryKeyForLanguage(String languageKey) {
    //TODO: decide whether to include language key in the repository id
	//return languageKey.toLowerCase() + "-" + KEY;
	return "roslyn." + REPOSITORY_ID;
  }

  public static String getRepositoryNameForLanguage(String languageKey) {
    //return languageKey.toUpperCase() + " " + NAME;
    return Plugin.NAME;
  }

}

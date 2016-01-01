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

  protected static final String KEY = "[PLUGIN_KEY]";
  protected static final String NAME = "[PLUGIN_NAME]";

  protected static final List<String> LANGUAGE_KEYS = Arrays.asList("[RULE_LANGUAGE]");

  public PluginRulesDefinition() {
  }

  protected String rulesDefinitionFilePath() {
    return "/resources/[RESOURCE_ID].rules.xml";
  }

  protected String sqaleDefinitionFilePath() {
    return "/resources/[RESOURCE_ID].sqale.xml";
  }

  private void defineRulesForLanguage(Context context, String repositoryKey, String repositoryName, String languageKey) {
    NewRepository repository = context.createRepository(repositoryKey, languageKey).setName(repositoryName);

    InputStream rulesXml = this.getClass().getResourceAsStream(rulesDefinitionFilePath());
    if (rulesXml == null)
	{
      LOG.info(KEY + ": rule information was not found");
	}
    else
	{
      LOG.info("Loading rule information from resource...");
      RulesDefinitionXmlLoader rulesLoader = new RulesDefinitionXmlLoader();
      rulesLoader.load(repository, rulesXml, "utf8"); // Charsets.UTF_8.name());
    }

	if (this.getClass().getResourceAsStream(sqaleDefinitionFilePath()) == null)
    {
      LOG.info(KEY + ": sqale information was not found");
    }
	else
	{
      LOG.info(KEY + ": loading Sqale information from resource...");
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
    //return languageKey.toLowerCase() + "-" + KEY;
	return KEY.toLowerCase();
  }

  public static String getRepositoryNameForLanguage(String languageKey) {
    //return languageKey.toUpperCase() + " " + NAME;
    return NAME;
  }

}

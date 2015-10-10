package myorg;

import java.io.InputStream;
import java.util.Arrays;
import java.util.List;

import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.api.server.rule.RulesDefinitionXmlLoader;

public final class [CLASS_NAME_PREFIX]RulesDefinition implements RulesDefinition {

  protected static final String KEY = "[PLUGIN_KEY]";
  protected static final String NAME = "[PLUGIN_NAME]";

  protected static final List<String> LANGUAGE_KEYS = Arrays.asList("[LANGUAGE]");

  public [CLASS_NAME_PREFIX]RulesDefinition() {
  }

  protected String rulesDefinitionFilePath() {
    return "/resources/rules.xml";
  }

  private void defineRulesForLanguage(Context context, String repositoryKey, String repositoryName, String languageKey) {
    NewRepository repository = context.createRepository(repositoryKey, languageKey).setName(repositoryName);

    InputStream rulesXml = this.getClass().getResourceAsStream(rulesDefinitionFilePath());
    if (rulesXml != null) {
      RulesDefinitionXmlLoader rulesLoader = new RulesDefinitionXmlLoader();
      rulesLoader.load(repository, rulesXml, "utf8"); // Charsets.UTF_8.name());
    }

    repository.done();
  }

  @Override
  public void define(Context context) {
    for (String languageKey : LANGUAGE_KEYS) {
      defineRulesForLanguage(context, TestRulesDefinition.getRepositoryKeyForLanguage(languageKey), TestRulesDefinition.getRepositoryNameForLanguage(languageKey),
              languageKey);
    }
  }

  public static String getRepositoryKeyForLanguage(String languageKey) {
    return languageKey.toLowerCase() + "-" + KEY;
  }

  public static String getRepositoryNameForLanguage(String languageKey) {
    return languageKey.toUpperCase() + " " + NAME;
  }

}

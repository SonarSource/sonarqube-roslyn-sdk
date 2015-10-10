package myorg;

import java.util.Arrays;
import java.util.List;

import org.sonar.api.SonarPlugin;

/**
 * This class is the entry point for all extensions
 */
public final class [CLASS_NAME_PREFIX]Plugin extends SonarPlugin {

  // This is where you're going to declare all your SonarQube extensions
  @Override
  public List getExtensions() {

    return Arrays.asList(
      // Definitions
      TestRulesDefinition.class
    );
  }
}

package myorg.[PLUGIN_KEY];

import java.util.Arrays;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.sonar.api.SonarPlugin;

/**
 * This class is the entry point for all extensions
 */
public final class Plugin extends SonarPlugin {

  private static final Logger LOG = LoggerFactory.getLogger(Plugin.class);

  // This is where you're going to declare all your SonarQube extensions
  @Override
  public List getExtensions() {

    LOG.info("Loading plugin [PLUGIN_KEY]");

    return Arrays.asList(
      // Definitions
      [CORE_EXTENSION_CLASS_LIST]
    );
  }
}

//-----------------------------------------------------------------------
// <copyright file="Plugin.java" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
package [PLUGIN_PACKAGE];

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

  protected static final String KEY = "[PLUGIN_KEY]";

  protected static final String NAME = "[PLUGIN_NAME]";

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

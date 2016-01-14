package [PLUGIN_PACKAGE];

import org.sonar.api.config.PropertyDefinition;

// Defines the metadata properties required by the C# plugin
public final class RoslynProperties {

  public static PropertyDefinition AnalyzerId = PropertyDefinition.builder(PluginRulesDefinition.KEY + ".analyzerId")
	.defaultValue("[ROSLYN_ANALYZER_ID]")
	.hidden()
	.build();

  public static PropertyDefinition RuleNamespace = PropertyDefinition.builder(PluginRulesDefinition.KEY + ".ruleNamespace")
	.defaultValue("[ROSLYN_RULE_NAMESPACE]")
	.hidden()
	.build();

  public static PropertyDefinition NuGetPackageId = PropertyDefinition.builder(PluginRulesDefinition.KEY + ".nuget.packageId")
	.defaultValue("[ROSLYN_NUGET_PACKAGE_ID]")
	.hidden()
	.build();

  public static PropertyDefinition NuGetPackageVersion = PropertyDefinition.builder(PluginRulesDefinition.KEY + ".nuget.packageVersion")
	.defaultValue("[ROSLYN_NUGET_PACKAGE_VERSION]")
	.hidden()
	.build();
}
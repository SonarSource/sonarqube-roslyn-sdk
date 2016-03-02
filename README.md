## Welcome to the SonarQube Roslyn SDK project

[![Build status](https://ci.appveyor.com/api/projects/status/q2gc65s7n4wjusp8/branch/master?svg=true)](https://ci.appveyor.com/project/SonarSource/sonarqube-roslyn-sdk/branch/master)

### Overview
This repo contains tools to help integrate Roslyn analyzers with SonarQube so that issues detected by the Roslyn analyzers are reported in SonarQube.
Specifically, the tools will generate a Java SonarQube plugin that registers the rules with SonarQube. See this [blog post](https://blogs.msdn.microsoft.com/visualstudioalm/2016/01/04/sonarqube-scanner-for-msbuild-v1-1-released-static-analysis-now-executed-during-the-build/) for more information.

### Target users
There are two groups of target users:

1. Roslyn analyzer authors
   - Analyzer authors will be able to use the tools during development to provide additional metadata about their rules (e.g. SonarQube severity, tags, a richer description, SQALE information) and generate the SonarQube plugin.

2. Roslyn analyzer users
   - If the analyzer author has not provided a SonarQube plugin for their analyzer then users will be able to generate a plugin from an analyzer NuGet package, although they won't be able to provide such rich metadata.

### Current status
The tooling is at an early stage and only works in limited scenarios. Also, the generated jar file is not currently sufficient on its own to upload Roslyn issues. The complete solution requires the [C# plugin v4.5](http://docs.sonarqube.org/display/PLUG/C%23+Plugin) or higher and the [MSBuild SonarQube Scanner v2.0](http://docs.sonarqube.org/display/SONAR/Analyzing+with+SonarQube+Scanner+for+MSBuild) or higher.


### Getting started

#### Current limitations:
   - the analyzer must be available as a NuGet package
   - the analyzer must use Roslyn v1.0 or v1.1
   - only C# rules are supported
   - the NuGet package must not require user acceptance of the license

These limitations will be addressed at some point in the future.

#### Pre-requisities
The SDK uses the Java compiler and jar packaging tool so you will need to run the SDK exe on a machine with version 7 of the [Java SE development kit](http://www.oracle.com/technetwork/java/javase/overview/index.html). The SonarQube site [lists](http://docs.sonarqube.org/display/SONAR/Requirements) the supported Java implementations. However, for the plugin to work correctly it should target the correct Java version, currently version 7. See [SFSRAP-35](https://jira.sonarsource.com/browse/SFSRAP-35) for more information. We'll improve how the SDK handles Java version mismatches, but for now you will need to make sure you target the correct version.

#### To generate a SonarQube plugin for an analyzer:
1. Build the SDK
  * Clone the repository
  * Build the solution *PluginGenerator.sln* in the repository root

2. Run the generator tool
  * Run the generator tool *RoslynSonarQubePluginGenerator.exe* located in *RoslynPluginGenerator\bin\[build flavour]\*, specifying the analyzer NuGet package id
  e.g. *RoslynSonarQubePluginGenerator /a:Wintellect.Analyzers*

It is possible to specify an optional package version
e.g. */a:Wintellect.Analyzers:1.0.5.0*

The tool will create a .jar file called based on the package name and version in the current directory
e.g. wintellectanalyzers-plugin-1.0.5.jar

The generated jar can be installed to SonarQube as normal (e.g. by dropping it in the SonarQube server *extensions\plugins* folder and restarting the SonarQube server).
You will see a new repository containing all of the rules defined by the analyzer. The rules can be added to Quality Profiles just like any other SonarQube rule.

#### NuGet packaging information

The SDK uses information from the NuGet package to populate the fields in the generated plugin that affect how the plugin is described in the Update Centre in the SonarQube UI. It is not currently possible to customise these values.

The NuGet package properties are documented [here](http://docs.nuget.org/Create/Nuspec-Reference) and the SonarQube plugin properties are documented [here](http://docs.sonarqube.org/display/DEV/Build+plugin).

The NuGet package properties are mapped to plugin properties as follows:

| Plugin property           | NuGet property | Falls back to |
|---------------------------|----------------|---------------|
| Plugin-Name               | title          | id            |
| Plugin-Description        | description    |               |
| Plugin-Version            | version        |               |
| Plugin-Developers         | authors        |               |
| Plugin-Organisation       | owners         | authors       |
| Plugin-Homepage           | projectUrl     |               |
| Plugin-TermsConditionsUrl | licenseUrl     |               |
| Plugin-License            | licenseNames** | licenseUrl    |
| Key*                      | id             |               |

\* This property is not visible to users, but must be unique. It is calculated from the package id.

\*\* This property is assigned heuristically by the NuGet.org website based on the licenseUrl.

#### Generating a jar for a private Roslyn analyzer
If you want to create a jar for Roslyn analyzer that is not available from a public NuGet feed (e.g. an analyzer you have created on your local machine) you can generate a jar file for it by specifying a package source that points at a local directory containing the *.nupkg* file created by the standard Roslyn templates. See the [NuGet docs](https://docs.nuget.org/create/hosting-your-own-nuget-feeds) for more information.

#### Configuring NuGet feeds
The SDK will look for NuGet.config files in the following locations:
- in the directory containing *RoslynSonarQubeGenerator.exe*
- %AppData%\NuGet (i.e. the standard pre-user location)
- %ProgramData%\NuGet\Config\SonarQube (a custom machine-wide location
- %ProgramData%\NuGet\Config (i.e. the standard machine-wide location)

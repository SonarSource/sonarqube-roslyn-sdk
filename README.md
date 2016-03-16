## Welcome to the SonarQube Roslyn SDK project

[![Build status](https://ci.appveyor.com/api/projects/status/q2gc65s7n4wjusp8/branch/master?svg=true)](https://ci.appveyor.com/project/SonarSource/sonarqube-roslyn-sdk/branch/master)

### Overview
This repo contains tools to help integrate Roslyn analyzers with SonarQube so that issues detected by the Roslyn analyzers are reported in SonarQube.
Specifically, the tools will generate a Java SonarQube plugin that registers the rules with SonarQube. The generated plugin works with the [C# plugin](http://docs.sonarqube.org/x/bAAW) (v4.5 or higher) and the [SonarQube Scanner for MSBuild](http://docs.sonarqube.org/x/Lx9q) (v2.0 or higher) to handle executing the analyzer and uploading any issues.
See this [blog post](https://blogs.msdn.microsoft.com/visualstudioalm/2016/02/18/sonarqube-scanner-for-msbuild-v2-0-released-support-for-third-party-roslyn-analyzers/) for more information.

### Download latest release
The latest release version (v1.0) is available [here](https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk/releases/download/1.0/SonarQube.Roslyn.SDK-1.0.zip).

### Target users
There are two groups of target users:

1. Roslyn analyzer authors
   - Analyzer authors will be able to use the tools during development to provide additional metadata about their rules (e.g. SonarQube severity, tags, a richer description, SQALE information) and generate the SonarQube plugin.

2. Roslyn analyzer users
   - If the analyzer author has not provided a SonarQube plugin for their analyzer then users will be able to generate a plugin from an analyzer NuGet package, although they won't be able to provide such rich metadata.

### Getting started

#### Current limitations:
   - the analyzer must be available as a NuGet package
   - the analyzer must use Roslyn v1.0 or v1.1
   - only C# rules are supported

These limitations will be addressed at some point in the future.

#### To generate a SonarQube plugin for an analyzer:
1. Download and install the latest [released version](https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk/releases/download/1.0/SonarQube.Roslyn.SDK-1.0.zip)
  
  Alternatively, if you want to build the SDK locally:
  * Clone the repository
  * Build the solution *PluginGenerator.sln* in the repository root

2. Run the generator tool
  * Run the generator tool *RoslynSonarQubePluginGenerator.exe* specifying the analyzer NuGet package id
  e.g. *RoslynSonarQubePluginGenerator /a:Wintellect.Analyzers*

It is possible to specify an optional package version
e.g. */a:Wintellect.Analyzers:1.0.5.0*

The tool will create a .jar file called based on the package name and version in the current directory
e.g. *wintellectanalyzers-plugin-1.0.5.jar*

The generated jar can be installed to SonarQube as normal (e.g. by dropping it in the SonarQube server *extensions\plugins* folder and restarting the SonarQube server).
You will see a new repository containing all of the rules defined by the analyzer. The rules can be added to Quality Profiles just like any other SonarQube rule.

#### Adding SQALE information to the generated plugin
The generator will create a template SQALE file that contains placeholders for the each rule in the analyzer being packaged. The template file will be created in the same folder as the generater .jar and will be named
*{package id}.{package version}.sqale.template.xml*.

If you want to provide SQALE information in the generated plugin, you can copy and manually edit the template file to contain the appropriate remediation information. Then generate the plugin again, this time specifying the */sqale:{filename}* option to tell the generator to embed the SQALE file in the plugin.

See the [SonarQube documentation](http://docs.sonarqube.org/display/SONAR/Technical+Debt) for more information about how SonarQube uses the SQALE method.


#### Generating a jar for a private Roslyn analyzer
If you want to create a jar for Roslyn analyzer that is not available from a public NuGet feed (e.g. an analyzer you have created on your local machine) you can generate a jar file for it by specifying a package source that points at a local directory containing the *.nupkg* file created by the standard Roslyn templates. See the [NuGet docs](https://docs.nuget.org/create/hosting-your-own-nuget-feeds) for more information.

#### Configuring NuGet feeds
The SDK will look for NuGet.config files in the following locations:
- in the directory containing *RoslynSonarQubeGenerator.exe*
- %AppData%\NuGet (i.e. the standard pre-user location)
- %ProgramData%\NuGet\Config\SonarQube (a custom machine-wide location
- %ProgramData%\NuGet\Config (i.e. the standard machine-wide location)

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

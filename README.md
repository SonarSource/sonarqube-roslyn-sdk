## Welcome to the SonarQube Roslyn SDK project

### License

Copyright 2015-2022 SonarSource.

Licensed under the [GNU Lesser General Public License, Version 3.0](http://www.gnu.org/licenses/lgpl.txt)

[![Build Status](https://dev.azure.com/sonarsource/DotNetTeam%20Project/_apis/build/status/SonarQube%20Roslyn%20Analyzer%20SDK?branchName=master)](https://dev.azure.com/sonarsource/DotNetTeam%20Project/_build/latest?definitionId=17&branchName=master)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=sonarqube-roslyn-sdk&metric=alert_status&token=5bf9d3f65527e95102fd8af7b5226c50dba35d66)](https://sonarcloud.io/dashboard?id=sonarqube-roslyn-sdk)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=sonarqube-roslyn-sdk&metric=coverage&token=5bf9d3f65527e95102fd8af7b5226c50dba35d66)](https://sonarcloud.io/dashboard?id=sonarqube-roslyn-sdk)

### Overview
This repo contains tools to help integrate Roslyn analyzers with SonarQube so that issues detected by the Roslyn analyzers are reported and managed in SonarQube.
Specifically, the tools will generate a Java SonarQube plugin that registers the rules with SonarQube. The generated plugin works with the [C# plugin](http://docs.sonarqube.org/x/bAAW) (v4.5 or higher) and the [SonarQube Scanner for MSBuild](http://docs.sonarqube.org/x/Lx9q) (v2.0 or higher) to handle executing the analyzer and uploading any issues.
See this [blog post](https://devblogs.microsoft.com/devops/sonarqube-scanner-for-msbuild-v2-0-released-support-for-third-party-roslyn-analyzers/) for more information.

#### Integration with the SonarQube Scanner for MSBuild
The [SonarQube Scanner for MSBuild](http://docs.sonarqube.org/x/Lx9q) will automatically execute your custom rules as part of an analysis build using the configuration you have defined in the Quality Profile. There is no need to manually reference your analyzer NuGet package in the MSBuild projects you want to analyse.

The SonarQube Scanner for MSBuild can also import issues from Roslyn analyzers that do not have plugins created using this SDK. However, they will be imported as "external issues" and are handled differently in SonarQube. See [Importing Issues from Third-Party Roslyn Analyzers (C#, VB.NET)](https://docs.sonarqube.org/pages/viewpage.action?pageId=11640944) for more information.

#### Integration with SonarLint for Visual Studio
If you define a Quality Profile that references your custom rules then [SonarLint for Visual Studio ](https://github.com/sonarsource/sonarlint-visualstudio) in *Connected Mode* will include those rules in the ruleset it generates.
However, it will **not** automatically configure Visual Studio to execute your custom rules.
If you want your customer rules to be executed inside Visual Studio then you will need reference your analyzer NuGet package from your MSBuild projects, or install your analyzer VSIX on each developer machine.

See the [SonarLint for Visual Studio wiki](https://github.com/SonarSource/sonarlint-visualstudio/wiki/Connected-Mode) for more information on Connected Mode.

### Download latest release
The latest release version (v2.0) is available [here](https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk/releases/download/2.0/SonarQube.Roslyn.SDK-2.0.zip).

### Compatibility
v1.0 of the SDK generates plugins that are compatible with SonarQube v4.5.2 -> v6.7.

v2.0 generates plugins that are compatible with versions of SonarQube from v6.7 (tested with the latest available version at time of release i.e. v7.3alpha1).

v3.0 generates plugins that are compatible with versions of SonarQube from v7.9.6 (tested with the latest available version at time of release i.e. v9.1).

v3.1 generates plugins that are compatible with versions of SonarQube from v7.9.6 (tested with the latest available version at time of release i.e. v9.7).


If you have an existing plugin that was generated with v1.0 of the SDK and want to use the plugin with SonarQube 7.0 or later, you will need to create a new plugin using v2.0 of the SDK. If you customized the _SQALE.xml_ file for your v1.0 plugin, you will need to move the remediation information to the _rules.xml_ file for the v2.0 plugin.

#### Current limitations:
   - the analyzer must be available as a NuGet package
   - the analyzer must use __Roslyn 2.8.2 or lower__ ~~(newer versions of Roslyn are not yet supported - see issue [SFSRAP-45](https://jira.sonarsource.com/browse/SFSRAP-45) for a workaround)~~
   - only C# rules are supported

#### Changes between v1.0 and v2.0
The full list of changes is contained is available on the [release page](https://github.com/SonarSource/sonarqube-roslyn-sdk/releases/tag/2.0). The main changes are described in more detail below.

* in v1.0, it was not possible to customize the _rules.xml_ file, although debt remediation information could be supplied in a separate _sqale.xml_ file. SQALE has been deprecated in SonarQube, and the format of the _rules.xml_ file has been extended to support debt remediation information. As a result, v2.0 of the SDK no longer supports providing a _sqale.xml_ file. Instead, it is now possible to manually edit the _rules.xml_ that describes the rule. This means debt remediation data can be added, and it also means that the rest of the metadata describing the rules can be edited to (e.g. to change the severity or classification or the rules, or to add tags).
* v2.0 is built against Roslyn 2.8.2, so will work against analyzers that use that version of Roslyn or earlier.
* v2.0 uses NuGet v4.7, which supports the TLS1.3 security protocol.

### Target users
There are two groups of target users:

1. Roslyn analyzer authors
   - Analyzer authors will be able to use the tools during development to provide additional metadata about their rules (e.g. SonarQube severity, tags, a richer description, ...) and generate the SonarQube plugin. See below for additional notes if you are developing your analyzer and running the SDK against the generated NuGet repeatedly on the same development machine.

2. Roslyn analyzer users
   - If the analyzer author has not provided a SonarQube plugin for their analyzer then users will be able to generate a plugin from an analyzer NuGet package, although they won't be able to provide such rich metadata.

### Getting started

#### To generate a SonarQube plugin for an analyzer:
1. Download and install the latest [released version](https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk/releases/download/2.0/SonarQube.Roslyn.SDK-2.0.zip)
  
  Alternatively, if you want to build the SDK locally:
  * Clone the repository
  * Build the solution *PluginGenerator.sln* in the repository root

2. Run the generator tool
  * Run the generator tool *RoslynSonarQubePluginGenerator.exe* specifying the analyzer NuGet package id
  e.g. *RoslynSonarQubePluginGenerator /a:Wintellect.Analyzers*

It is possible to specify an optional package version
e.g. */a:Wintellect.Analyzers:1.0.5.0*

The tool will create a .jar file named after the package name and version in the current directory
e.g. *wintellectanalyzers-plugin-1.0.5.jar*
You can specify the output directory with the `/o:PathToOutputDir` command line parameter.

The generated jar can be installed to SonarQube as normal (e.g. by dropping it in the SonarQube server *extensions\plugins* folder and restarting the SonarQube server).
You will see a new repository containing all of the rules defined by the analyzer. The rules can be added to Quality Profiles just like any other SonarQube rule.

#### Customizing the rules.xml file
To customize the _rules.xml_ file, run the generator once against the NuGet package. The generator will produce a template _rules.xml_ for the analyzers found in the package as well as producing the .jar file. Edit the _rules.xml_ file then run the generator tool again, this time providing the _/rules_ parameter to point to the edited _rules.xml_ file.

The XML snippet below shows the expected format for tags and debt remediation information.

```xml
<?xml version="1.0" encoding="utf-8"?>
<rules>
  <rule>
    <key>S1000</key>
    <name>My title</name>
    <severity>BLOCKER|CRITICAL|MAJOR|MINOR|INFO</severity>
    <cardinality>SINGLE</cardinality>
    <description><![CDATA[My description]]></description>
    <tag>my-first-tag</tag>
    <tag>my-second-tag</tag>
    <type>BUG</type>
    <debtRemediationFunction>CONSTANT_ISSUE</debtRemediationFunction>
    <debtRemediationFunctionOffset>15min</debtRemediationFunctionOffset>
  </rule>
</rules>
```


#### Configuring NuGet feeds
The SDK will look for NuGet.config files in the following locations:
- in the directory containing *RoslynSonarQubeGenerator.exe*
- %AppData%\NuGet (i.e. the standard pre-user location)
- %ProgramData%\NuGet\Config\SonarQube (a custom machine-wide location
- %ProgramData%\NuGet\Config (i.e. the standard machine-wide location)

If the analyzer you want to package is available in a private NuGet feed then you will need to create an appropriate NuGet.config file to point to the private feed. Alternatively you can use the `/customnugetrepo:file:///PathToRepo` 
parameter. This will overwrite the above mentioned NuGet behaviour.

#### Generating a jar for an analyzer that is not available from a NuGet feed
If you want to create a jar for Roslyn analyzer that is not available from a NuGet feed (e.g. an analyzer you have created on your local machine) you can specify a package source that points at a local directory containing the *.nupkg* file created by the standard Roslyn templates. See the [NuGet docs](https://docs.nuget.org/create/hosting-your-own-nuget-feeds) for more information.

#### NuGet packaging information

The SDK uses information from the NuGet package to populate the fields in the generated plugin that affect how the plugin is described in the Update Centre in the SonarQube UI. It is not currently possible to customise these values.

The NuGet package properties are documented [here](http://docs.nuget.org/Create/Nuspec-Reference) and the SonarQube plugin properties are documented [here](http://docs.sonarqube.org/x/JQxq).

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


### Additional notes for Roslyn analyzer authors
The SDK caches the NuGet packages it downloads locally under `%temp%\.sonarqube.sdk\.nuget`.

This matters if you are developing your analyzer iteratively on a development machine i.e. with the following workflow:
> change analyzer code -> build package -> deploy package to local NuGet feed -> run SDK against the new package -> repeat

In that case, you have to do one of the following before running the SDK:
* change the package version number, or
* delete your package from the local cache (`%temp%\.sonarqube.sdk\.nuget`).

If you don't, the SDK exe will use the cached version, rather than the version you have just built.

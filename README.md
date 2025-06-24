# Welcome to the SonarQube Roslyn SDK project

## License

Test

Copyright 2015-2025 SonarSource.

Licensed under the [GNU Lesser General Public License, Version 3.0](http://www.gnu.org/licenses/lgpl.txt)

[![Build Status](https://dev.azure.com/sonarsource/DotNetTeam%20Project/_apis/build/status/SonarQube%20Roslyn%20Analyzer%20SDK?branchName=master)](https://dev.azure.com/sonarsource/DotNetTeam%20Project/_build/latest?definitionId=17&branchName=master)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=sonarqube-roslyn-sdk&metric=alert_status&token=5bf9d3f65527e95102fd8af7b5226c50dba35d66)](https://sonarcloud.io/dashboard?id=sonarqube-roslyn-sdk)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=sonarqube-roslyn-sdk&metric=coverage&token=5bf9d3f65527e95102fd8af7b5226c50dba35d66)](https://sonarcloud.io/dashboard?id=sonarqube-roslyn-sdk)

## Overview
This repo contains tools to help integrate Roslyn analyzers with SonarQube so that issues detected by the Roslyn analyzers are reported and managed in SonarQube.
Specifically, the tools will generate a Java SonarQube plugin that registers the rules with SonarQube. The generated plugin works with the [C# plugin](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/languages/csharp/), [VB.NET plugin](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/languages/vb-dotnet/) and the [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/scanners/sonarscanner-for-dotnet/) to handle executing the analyzer and uploading any issues.
See this [blog post](https://devblogs.microsoft.com/devops/sonarqube-scanner-for-msbuild-v2-0-released-support-for-third-party-roslyn-analyzers/) for more information.

### Integration with the SonarScanner for .NET
The [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/scanners/sonarscanner-for-dotnet/) will automatically execute your custom rules as part of an analysis build using the configuration you have defined in the Quality Profile. There is no need to manually reference your analyzer NuGet package in the MSBuild projects you want to analyse.

The SonarScanner for .NET can also import issues from Roslyn analyzers that do not have plugins created using this SDK. However, they will be imported as "external issues" and are handled differently in SonarQube. See [External .NET issues](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/importing-external-issues/external-analyzer-reports/#external-dotnet-issues) for more information.

### Integration with SonarLint for Visual Studio
If you define a Quality Profile that references your custom rules then [SonarLint for Visual Studio](https://github.com/sonarsource/sonarlint-visualstudio) in *Connected Mode* will include those rules in the ruleset it generates.
However, it will **not** automatically configure Visual Studio to execute your custom rules.
If you want your customer rules to be executed inside Visual Studio then you will need reference your analyzer NuGet package from your MSBuild projects, or install your analyzer VSIX on each developer machine.

See the [SonarLint for Visual Studio documentation](https://docs.sonarsource.com/sonarlint/visual-studio/team-features/connected-mode/) for more information on Connected Mode.

## Getting started

1. Download the latest [released version](https://github.com/SonarSource/sonarqube-roslyn-sdk/releases/latest), or clone and build this repository.
  
1. Run the generator tool `RoslynSonarQubePluginGenerator.exe` specifying the analyzer NuGet package id
    ```
    RoslynSonarQubePluginGenerator.exe /a:YourAnalyzerNugetPackageId
    ```

1. Copy the generated `youranalyzernugetpackageid-plugin.1.0.0.jar` into your SonarQube `extensions\plugins` directory.

1. Restart your SonarQube server.

1. Add the rules to a Quality Profile in SonarQube.

1. Configure your SonarQube project to use the new Quality Profile.

1. Run an analysis using the [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube/latest/analyzing-source-code/scanners/sonarscanner-for-dotnet/).

You can run `RoslynSonarQubePluginGenerator.exe` without parameters to see all the available command line options.

## Compatibility

| SDK | Minimal compatible SQ | Tested with SQ |
|-----|-----------------------|----------------|
| 1.0 | 4.5.2                 | 6.7            |
| 2.0 | 6.7                   | 7.3alpha1      |
| 3.0 | 7.9.6                 | 9.1            |
| 3.1 | 7.9.6                 | 9.7            |
| 4.0 | 9.9                   | 10.5.1         |

Plugins generated with a specific SDK version will not work with SonarQube versions older than the minimal compatible version. The latest version available for testing at the time of the SDK release is indicated in the *Tested with SQ* column. 


## Updating compatible Roslyn version

The SDK is compatible with analyzer targeting Roslyn from version 1.0.0 up to the version specified in [Directory.Build.props](./Directory.Build.props#L10).

To support a newer version of Roslyn, follow these steps:
1. Find the latest version of Roslyn on [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis).
1. Update the version in the [Directory.Build.props](./Directory.Build.props). 
1. Rebuild the SDK.
1. Run the SDK against your analyzer.

## Target users
There are two groups of target users:

1. Roslyn analyzer authors

   Analyzer authors will be able to use the tools during development to provide additional metadata about their rules (e.g. SonarQube severity, tags, a richer description, ...) and generate the SonarQube plugin. See below for additional notes if you are developing your analyzer and running the SDK against the generated NuGet repeatedly on the same development machine.

1. Roslyn analyzer users

   If the analyzer author has not provided a SonarQube plugin for their analyzer then users will be able to generate a plugin from an analyzer NuGet package, although they won't be able to provide such rich metadata.

## Advanced scenarios

### Customizing the rules.xml file
To customize the `rules.xml` file, run the generator once against the NuGet package. The generator will produce a template `rules.xml` for the analyzers found in the package as well as producing the .jar file. Edit the `rules.xml` file then run the generator tool again, this time providing the `/rules` parameter to point to the edited `rules.xml` file.

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

### Configuring NuGet feeds
The SDK will look for NuGet.config files in the following locations:
- in the directory containing `RoslynSonarQubeGenerator.exe`
- `%AppData%\NuGet` - the standard pre-user location)
- `%ProgramData%\NuGet\Config\SonarQube` - a custom machine-wide location
- `%ProgramData%\NuGet\Config` - the standard machine-wide location

If the analyzer you want to package is available in a private NuGet feed then you will need to create an appropriate `NuGet.config` file to point to the private feed. Alternatively you can use the `/customnugetrepo:file:///PathToRepo` parameter. This will overwrite the above mentioned NuGet behaviour.

### Generating a jar for an analyzer that is not available from a NuGet feed
If you want to create a jar for Roslyn analyzer that is not available from a NuGet feed (e.g. an analyzer you have created on your local machine) you can specify a package source that points at a local directory containing the *.nupkg* file created by the standard Roslyn templates. See the [NuGet docs](https://learn.microsoft.com/en-us/nuget/hosting-packages/overview) for more information.

By default, the [NuGet.config](./RoslynPluginGenerator/NuGet.config#L16) file shipped with the RoslynSonarQubeGenerator has a local package source configured that points to `C:\LocalNugetFeed`.

### NuGet packaging information

The SDK uses information from the NuGet package to populate the fields in the generated plugin that affect how the plugin is described in the Update Centre in the SonarQube UI. It is not currently possible to customise these values.

The NuGet package properties are documented [here](https://learn.microsoft.com/en-us/nuget/reference/nuspec#required-metadata-elements) and the SonarQube plugin properties are documented [here](https://docs.sonarsource.com/sonarqube/latest/extension-guide/developing-a-plugin/plugin-basics/#advanced-build-properties).

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

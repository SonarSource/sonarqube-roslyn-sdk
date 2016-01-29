## Welcome to the SonarQube Roslyn SDK project

### Overview
This repo contains tools to help integrate Roslyn analyzers with SonarQube so that issues detected by the Roslyn analyzers are reported in SonarQube.
Specifically, the tools will generate a Java SonarQube plugin that registers the rules with SonarQube.

### Target users
There are two groups of target users:

1. Roslyn analyzer authors
   - Analyzer authors will be able to use the tools during development to provide additional metadata about their rules (e.g. SonarQube severity, tags, a richer description, SQALE information) and generate the SonarQube plugin.

2. Roslyn analyzer users
   - If the analyzer author has not provided a SonarQube plugin for their analyzer then users will be able to generate a plugin from an analyzer NuGet package, although they won't be able to provide such rich metadata.

### Current status
The tooling is at a very early stage and only works in limited scenarios. Also, the generated jar file is not currently sufficient on its own to upload Roslyn issues. The complete solution depends on changes to the underlying C# plugin and MSBuild SonarQube Scanner that will be made in future releases of those products.


### Getting started

#### Current limitations:
   - the analyzer package must be available at https://www.nuget.org/
   - the analyzer must use Roslyn 1.0
   - only C# rules are supported

These limitations will be addressed at some point in the future.


#### To generate a SonarQube plugin for an analyzer:

1. Build the SDK
  * Clone the repository
  * Build the solution *PluginGenerator.sln* in the repository root

2. Run the generator tool
  * The SDK uses the Java compiler and jar packaging tool, so you will need a machine with a recent version of the [Java SE development kit](http://www.oracle.com/technetwork/java/javase/overview/index.html).
  * Run the generator tool *SonarQube.Plugin.Roslyn.PluginGenerator.exe* located in *RoslynPluginGenerator\bin\[build flavour]\*, specifying the analyzer NuGet package id
  e.g. *SonarQube.Plugin.Roslyn.PluginGenerator /a:Wintellect.Analyzers*

It is possible to specify an optional package version
e.g. */a:Wintellect.Analyzers:1.0.5.0*

The tool will create a .jar file called *[package id]-plugin-[package version].jar* in the current directory
e.g. Wintellect.Analyzers-plugin-1.0.5.jar

The generated jar can be installed to SonarQube as normal (e.g. by dropping it in the SonarQube server *extensions\plugins* folder and restarting the SonarQube server).
You will see a new repository containing all of the rules defined by the analyzer. The rules can be added to Quality Profiles just like any other SonarQube rule.

#### NuGet packing information:

The SDK uses information from NuGet to populate relevant fields in the generated plugin. The following NuGet properties are mapped:    

| NuGet property | Plugin property |
|---|---|
| title | Name |
| description | Description |
| version | Version |
| authors | Developers |
| owners | Organisation |
| projectUrl | Homepage |
| id | Key* |    

\* This property is not visible to users, but must be unique.

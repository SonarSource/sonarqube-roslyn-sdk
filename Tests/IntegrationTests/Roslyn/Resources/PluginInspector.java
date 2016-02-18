//-----------------------------------------------------------------------
// <copyright file="PluginInspector.java" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
import org.sonar.api.SonarPlugin;
import org.sonar.api.config.PropertyDefinition;
import org.sonar.api.server.rule.RulesDefinition;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.dom.DOMSource;
import javax.xml.transform.stream.StreamResult;
import java.io.File;
import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.net.MalformedURLException;
import java.net.URL;
import java.net.URLClassLoader;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.jar.Attributes;
import java.util.jar.Manifest;

/**
 * Simple console app used to test generated SonarQube plugin jar files.
 * The app attempts to exercise the specified jar file by loading it
 * and asking the plugin class for the defined extensions.
 * An xml file is created containing information about the plugin e.g.
 * the contents of the manifest and details of the extensions.
 * Inputs:
 *  1) Path to the jar file (required)
 *  2) Full path of the xml file containing the output (optional)
 *      If the second argument is not supplied then the output file name
 *      is the jar file suffixed with ".dump.xml".
 */
public class PluginInspector  {

    private static final int SuccessCode = 0;
    private static final int ErrorCode = 1;

    public static void main(String[] args) throws Exception {

        Log("Starting Plugin inspector v0.1...");
        String jarPath = null;

        if (args.length != 1 && args.length != 2){
            Log("Expecting at least one argument: the full path to the jar file.\r\nOptionally, the name of the file to be created may also be supplied.");
            System.exit(ErrorCode);
        }
        else{
            jarPath = args[0];
        }

        File file = new File(jarPath);
        if (!file.exists()){
            Log("The specified file does not exist: " + jarPath);
            System.exit(ErrorCode);
        }

        String xmlFilePath = args.length == 2 ? args[1] : jarPath + ".dump.xml";

        Integer exitCode = CreateAndInvokeInspector(jarPath, xmlFilePath);

        System.exit(exitCode.intValue());
    }

    public static Integer CreateAndInvokeInspector(String jarPath, String xmlFilePath) throws MalformedURLException, ClassNotFoundException, NoSuchMethodException, InvocationTargetException, IllegalAccessException {
        // Create a class loader that will be used to load the plugin jar and
        // // all of the required SonarQube jars. It will also be able to
        // load the resources from the jar.
        // We're doing this to work round the fact that the SqaleXmLoader uses
        // it's class loader to locate the embedded SQALE file (unlike the Rule
        // reader that accepts an InputStream). This means that the class loader
        // that created the SqaleXmlLoader must be able to locate resources embedded
        // in the plugin. So, we'll create a class loader that can find everything
        // without delegating to the main App class loader for the executable
        URLClassLoader loader = CreatePluginLoader(jarPath);

        Class<?> inspectorClass = loader.loadClass("PluginInspector");
        Object inspector = TryCreateInstance(inspectorClass);

        // The inspector has been created using a different class loader so we
        // can't cast it to PluginInspector. Instead, we have to invoke the
        // method using reflection.
        Method method = inspectorClass.getMethod("Invoke", String.class, String.class);
        Integer exitCode = (Integer)method.invoke(inspector, jarPath, xmlFilePath);
        return exitCode;
    }

    private static URLClassLoader CreatePluginLoader(String jarPath) throws MalformedURLException {

        // Creates and returns a class loader that can load the specified jar and all
        // of the jars that the App plugin loader can handle, but that is not
        // parented to the App plugin loader
        URL jarUrl = new URL("file:/" + jarPath);

        // Hard-cast to URLClassLoader
        URLClassLoader parentUrlLoader = (URLClassLoader)PluginInspector.class.getClassLoader();

        // Create a list containing all of the parent URLs together with the specified jar URL
        ArrayList<URL> allUrls = new ArrayList<URL>();
        allUrls.addAll(Arrays.asList(parentUrlLoader.getURLs()));
        allUrls.add(jarUrl);
        URL[] urlArray = allUrls.toArray(new URL[0]);

        URLClassLoader loader = new URLClassLoader(urlArray, parentUrlLoader.getParent());
        return loader;
    }

    public Integer Invoke(String jarPath, String xmlFilePath) throws Exception {

        ClassLoader pluginLoader = this.getClass().getClassLoader();
        Document doc = this.ProcessJar(jarPath, pluginLoader);

        Integer exitCode = SuccessCode;
        if (doc == null) {
            exitCode = ErrorCode;
        }
        else {
            SaveToFile(doc, xmlFilePath);
        }
        return exitCode;
    }

    private Document ProcessJar(String jarPath, ClassLoader pluginLoader) throws MalformedURLException, ClassNotFoundException {
        Manifest manifest = TryGetManifest(jarPath);

        if (manifest == null)
        {
            Log("Failed to retrieve the manifest");
            return null;
        }

        Attributes mainAttrs = manifest.getMainAttributes();
        String pluginClassName = mainAttrs.getValue("Plugin-Class");

        if (pluginClassName == null){
            Log("The manifest does not specify a Plugin-Class");
            return null;
        }
        Log("Plugin class to be created: " + pluginClassName);

        Document doc = CreateNewDocument();
        Element root = doc.createElement("JarInfo");
        doc.appendChild(root);
        root.setAttribute("jarPath", jarPath);

        ProcessManifest(mainAttrs, doc, root);

        Log("Creating plugin dynamically...");
        Class<?> pluginClass = pluginLoader.loadClass(pluginClassName);
        SonarPlugin plugin = TryCreateInstance(pluginClass);

        if (plugin != null) {
            System.out.println("Plugin created");

            ProcessPlugin(root, plugin);
        }
        return doc;
    }

    private static Manifest TryGetManifest(String jarPath)
    {
        Manifest manifest = null;
        String mf = "jar:file:/" + jarPath + "!/META-INF/MANIFEST.MF";

        URL mfUrl = null;
        try {
            mfUrl = new URL(mf);
            try {
                manifest = new Manifest(mfUrl.openStream());
            } catch (IOException e) {
                e.printStackTrace();
            }
        } catch (MalformedURLException e) {
            e.printStackTrace();
        }

        return manifest;
    }

    private static void ProcessManifest(Attributes mainAttrs, Document doc, Element root) {
        Log("Processing manifest...");

        Element manifestEl = doc.createElement("Manifest");
        root.appendChild(manifestEl);

        for(Object key : mainAttrs.keySet()) {
            Object value = mainAttrs.get(key);

            Element manifestItem = doc.createElement("Item");
            manifestEl.appendChild(manifestItem);

            manifestItem.setAttribute("key", key.toString());
            manifestItem.setAttribute("value", value.toString());
        }
    }

    private static void ProcessPlugin(Element root, SonarPlugin plugin) {
        System.out.println("Getting extensions...");
        List extensions = plugin.getExtensions();
        System.out.println("Extensions retrieved");

        Element extensionsEl = CreateNewChild("Extensions", root);

        for(Object extension: extensions)
        {
            DumpExtension(extension, extensionsEl);
        }
    }

    private static void DumpExtension(Object extension, Element extensionsEl)
    {
        Log("extension: " + extension);

        boolean recognised = false;
        if (extension instanceof PropertyDefinition)
        {
            DumpExtension((PropertyDefinition)extension, extensionsEl);
            recognised = true;
        }
        else if (extension.getClass() == Class.class)
        {
            Class c = (Class)extension;

            if(Arrays.asList(c.getInterfaces()).contains(RulesDefinition.class)) {
                RulesDefinition def  = TryCreateInstance(c);
                DumpExtension(def, extensionsEl);
                recognised = true;
            }
        }

        if (!recognised){
            CreateExtensionElement(extension, "Unknown", extensionsEl);
        }
    }

    private static void DumpExtension(PropertyDefinition definition, Element extensionsEl)
    {
        Log("extension=property - key: " + definition.key() + " default: " + definition.defaultValue());

        Element extensionEl = CreateExtensionElement(definition, "PropertyDefinition", extensionsEl);
        extensionEl.setAttribute("key", definition.key());
        extensionEl.setAttribute("defaultValue", definition.defaultValue());
    }

    private static void DumpExtension(RulesDefinition definition, Element extensionsEl)
    {
        Element extensionEl = CreateExtensionElement(definition, "RulesDefinition", extensionsEl);

        RulesDefinition.Context context = new RulesDefinition.Context();
        definition.define(context);

        Log("extension=rules: " + definition.toString());

        for(RulesDefinition.Repository repo : context.repositories())
        {
            Log("  repo - key: " + repo.key() + " name: " + repo.name() + " count: " + repo.rules().size());

            Element repoEl = CreateNewChild("Repository", extensionEl);
            extensionEl.appendChild(repoEl);
            repoEl.setAttribute("key", repo.key());
            repoEl.setAttribute("name", repo.name());
            repoEl.setAttribute("language", repo.language());
//            item.setAttribute("ruleCount", repo.rules().size());

            Element rulesEl = CreateNewChild("Rules", repoEl);

            for(RulesDefinition.Rule r : repo.rules())
            {
                Log("  rule:  " + r.key() + " " + r.internalKey());

                Element ruleEl = CreateNewChild("Rule", rulesEl);
                ruleEl.setAttribute("key", r.key());
                ruleEl.setAttribute("name", r.name());
                ruleEl.setAttribute("internalKey", r.internalKey());
                ruleEl.setAttribute("severity", r.severity());
            }
        }
    }

    private static void Log(String message)
    {
        System.out.println(message);
    }

    private static <T>  T TryCreateInstance(Class type)
    {
        Log("Creating instance of " + type.getName());
        T instance = null;
        try {
            instance = (T) type.newInstance();
        } catch (Exception e) {
            Log("Failed to create instance dynamically: " + e.getMessage());
        }
        return instance;
    }

    private static Document CreateNewDocument(){
        DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();

        Document newDoc = null;
        try {
            DocumentBuilder builder = factory.newDocumentBuilder();
            newDoc = builder.newDocument();
        } catch (ParserConfigurationException e) {
            e.printStackTrace();
        }
        return newDoc;
    }

    private static Element CreateNewChild(String name, Element parent)
    {
        Element child = parent.getOwnerDocument().createElement(name);
        parent.appendChild(child);
        return child;
    }

    private static Element CreateExtensionElement(Object extension, String type, Element extensionsEl)
    {
        Class extensionClass;
        if (extension instanceof Class){
            extensionClass = (Class)extension;
        }
        else{
            extensionClass = extension.getClass();
        }

        Element extensionEl = CreateNewChild(type, extensionsEl);
        extensionEl.setAttribute("type", type);
        extensionEl.setAttribute("class", extensionClass.getName());

        return extensionEl;
    }

    private static void SaveToFile(Document doc, String filePath) throws Exception{
        Log("Saving file to " + filePath);
        TransformerFactory factory = TransformerFactory.newInstance();
        Transformer transformer = factory.newTransformer();

        StreamResult output = new StreamResult(new File(filePath));
        transformer.transform(new DOMSource(doc), output);
        Log("File saved");
    }
}

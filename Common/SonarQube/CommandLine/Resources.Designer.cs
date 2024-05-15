﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarQube.Common {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarQube.Plugins.Common.SonarQube.CommandLine.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to   {0,-25}{1}.
        /// </summary>
        internal static string CmdLine_Help_Argument {
            get {
                return ResourceManager.GetString("CmdLine_Help_Argument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Optional arguments:.
        /// </summary>
        internal static string CmdLine_Help_OptionalArguments {
            get {
                return ResourceManager.GetString("CmdLine_Help_OptionalArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Required argument:.
        /// </summary>
        internal static string CmdLine_Help_RequiredArguments {
            get {
                return ResourceManager.GetString("CmdLine_Help_RequiredArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A value has already been supplied for this argument: {0}. Existing: &apos;{1}&apos;.
        /// </summary>
        internal static string ERROR_CmdLine_DuplicateArg {
            get {
                return ResourceManager.GetString("ERROR_CmdLine_DuplicateArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A required argument is missing: {0}.
        /// </summary>
        internal static string ERROR_CmdLine_MissingRequiredArgument {
            get {
                return ResourceManager.GetString("ERROR_CmdLine_MissingRequiredArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unrecognized command line argument: {0}.
        /// </summary>
        internal static string ERROR_CmdLine_UnrecognizedArg {
            get {
                return ResourceManager.GetString("ERROR_CmdLine_UnrecognizedArg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Descriptor ids must be unique.
        /// </summary>
        internal static string ERROR_Parser_UniqueDescriptorIds {
            get {
                return ResourceManager.GetString("ERROR_Parser_UniqueDescriptorIds", resourceCulture);
            }
        }
    }
}

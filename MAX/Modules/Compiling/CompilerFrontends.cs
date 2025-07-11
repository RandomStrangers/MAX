/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified by MCGalaxy)

    Edited for use with MCGalaxy
 
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System.Collections.Generic;
namespace MAX.Compiling
{
    public class CSCompiler : ICompiler
    {
        public override string FileExtension { get { return ".cs"; } }
        public override string ShortName { get { return "C#"; } }
        public override string FullName { get { return "CSharp"; } }

#if !MAX_DOTNET
        public override ICompilerErrors DoCompile(string[] srcPaths, string dstPath)
        {
            List<string> referenced = ProcessInput(srcPaths, "//");

            OrderLineCompiler compiler = new ClassicCSharpCompiler();
            return compiler.Compile(srcPaths, dstPath, referenced);
        }
#else
        public override ICompilerErrors DoCompile(string[] srcPaths, string dstPath) {
            List<string> referenced = ProcessInput(srcPaths, "//");
            referenced.Add("System.Collections.dll");    // needed for List<> etc
            referenced.Add("System.IO.Compression.dll"); // needed for GZip compression
            referenced.Add("System.Net.Primitives.dll"); // needed for IPAddress etc

            OrderLineCompiler compiler = new RoslynCSharpCompiler();
            return compiler.Compile(srcPaths, dstPath, referenced);
        }

        public override void ProcessInputLine(string line, List<string> referenced) {
            if (!line.CaselessStarts("//dotnetref")) return;

            referenced.Add(GetDLL(line));
        }
#endif

        public override string OrderSkeleton
        {
            get
            {
                return @"//\tAuto-generated order skeleton class
//\tUse this as a basis for custom MAX orders
//\tNaming should be kept consistent (e.g. /update order should have a class name of 'OrdUpdate' and a filename of 'OrdUpdate.cs')
// As a note, MAX is designed for .NET 4.8

// To reference other assemblies, put a ""//reference [assembly filename]"" at the top of the file
//   e.g. to reference the System.Data assembly, put ""//reference System.Data.dll""

// Add any other using statements you need after this
using System;
using MAX;

public class Ord{0} : Order
{{
\t// The order's name (what you put after a slash to use this order)
\tpublic override string Name {{ get {{ return ""{0}""; }} }}

\t// Order's shortcut, can be left blank (e.g. ""/Copy"" has a shortcut of ""c"")
\tpublic override string Shortcut {{ get {{ return """"; }} }}

\t// Which submenu this order displays in under /Help
\tpublic override string Type {{ get {{ return OrderTypes.Other; }} }}

\t// Whether or not this order can be used in a museum. Block/map altering orders should return false to avoid errors.
\tpublic override bool MuseumUsable {{ get {{ return true; }} }}

\t// The default rank required to use this order. Valid values are:
\t//   LevelPermission.Guest, LevelPermission.Builder, LevelPermission.AdvBuilder,
\t//   LevelPermission.Operator, LevelPermission.Admin, LevelPermission.Owner
\tpublic override LevelPermission DefaultRank {{ get {{ return LevelPermission.Guest; }} }}

\t// This is for when a player executes this order by doing /{0}
\t//   p is the player object for the player executing the order. 
\t//   message is the arguments given to the order. (e.g. for '/{0} this', message is ""this"")
\tpublic override void Use(Player p, string message)
\t{{
\t\tp.Message(""Hello World!"");
\t}}

\t// This is for when a player does /Help {0}
\tpublic override void Help(Player p)
\t{{
\t\tp.Message(""/{0} - Does stuff. Example order."");
\t}}
}}";
            }
        }

        public override string AddonSkeleton
        {
            get
            {
                return @"//\tAuto-generated addon skeleton class
//\tUse this as a basis for custom MAX addons

// To reference other assemblies, put a ""//reference [assembly filename]"" at the top of the file
//   e.g. to reference the System.Data assembly, put ""//reference System.Data.dll""

// Add any other using statements you need after this
using System;

namespace MAX
{{
\tpublic class {0} : Addon
\t{{
\t\t// The addon's name (i.e what shows in /Addons)
\t\tpublic override string Name {{ get {{ return ""{0}""; }} }}

\t\t// The oldest version of MAX this addon is compatible with
\t\tpublic override string MAX_Version {{ get {{ return ""{2}""; }} }}

\t\t// Message displayed in server logs when this addon is loaded
\t\tpublic override string Welcome {{ get {{ return ""Loaded Message!""; }} }}

\t\t// Who created/authored this addon
\t\tpublic override string Creator {{ get {{ return ""{1}""; }} }}

\t\t// Called when this addon is being loaded (e.g. on server startup)
\t\tpublic override void Load(bool startup)
\t\t{{
\t\t\t//code to hook into events, load state/resources etc goes here
\t\t}}

\t\t// Called when this addon is being unloaded (e.g. on server shutdown)
\t\tpublic override void Unload(bool shutdown)
\t\t{{
\t\t\t//code to unhook from events, dispose of state/resources etc goes here
\t\t}}

\t\t// Displays help for or information about this addon
\t\tpublic override void Help(Player p)
\t\t{{
\t\t\tp.Message(""No help is available for this addon."");
\t\t}}
\t}}
}}";
            }
        }
    }
}
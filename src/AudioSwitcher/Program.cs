// -----------------------------------------------------------------------
// Copyright (c) David Kean.
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using AudioSwitcher.ApplicationModel;

namespace AudioSwitcher
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                IApplication application = container.GetExportedValue<IApplication>();

                application.Args = new Dictionary<string,string>();
                if (args.Length > 0)
                {
                    foreach(var arg in args[0].Split('|'))
                    {
                        var split = arg.Split('=');
                        application.Args.Add(split[0], split[1]);
                    }
                }

                application.Start();
            }
        }
    }
}

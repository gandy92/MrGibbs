﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Ninject.Modules;

using PovertySail.Contracts;

namespace PovertySail.Configuration
{
    public class PluginModule:NinjectModule
    {
        public override void Load()
        {
            //force the contracts dll to load
            var contract = new PovertySail.Contracts.PluginConfiguration();

            string path = Assembly.GetExecutingAssembly().Location;
            FileInfo file = new FileInfo(path);
            DirectoryInfo directory = file.Directory;

            foreach (var otherPath in directory.GetFiles("PovertySail.*.dll"))
            {
                Assembly assembly = Assembly.LoadFrom(otherPath.FullName);

                var types = assembly.GetExportedTypes();
                foreach (var type in types)
                {
                    if (type.GetInterface(typeof(IPlugin).Name)!=null && type.IsClass && !type.IsInterface && !type.IsAbstract)
                    {
                        Kernel.Bind<IPlugin>().To(type).InSingletonScope();
                    }
                }
            }
        }
    }
}

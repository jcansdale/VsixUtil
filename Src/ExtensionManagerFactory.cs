﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.ExtensionManager;

namespace VsixUtil
{
    public sealed class ExtensionManagerFactory
    {
        private static Assembly LoadImplementationAssembly(Version version)
        {
            var format = "Microsoft.VisualStudio.ExtensionManager.Implementation, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            var strongName = string.Format(format, CommonUtil.GetVersionNumber(version));
            return Assembly.Load(strongName);
        }

        private static Assembly LoadSettingsAssembly(Version version)
        {
            var format = "Microsoft.VisualStudio.Settings{0}, Version={1}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            string suffix = "";
            switch (version)
            {
                case Version.Vs2010:
                    suffix = "";
                    break;
                case Version.Vs2012:
                    suffix = ".11.0";
                    break;
                case Version.Vs2013:
                    suffix = ".12.0";
                    break;
                case Version.Vs2015:
                    suffix = ".14.0";
                    break;
                case Version.Vs2017:
                    suffix = ".15.0";
                    break;
                default:
                    throw new Exception("Bad Version");
            }

            var strongName = string.Format(format, suffix, CommonUtil.GetVersionNumber(version));
            return Assembly.Load(strongName);
        }

        private static Type GetExtensionManagerServiceType(Version version)
        {
            var assembly = LoadImplementationAssembly(version);
            return assembly.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService");
        }

        public static IVsExtensionManager CreateExtensionManager(string applicationPath, Version version, string rootSuffix)
        {
            var settingsAssembly = LoadSettingsAssembly(version);

            var externalSettingsManagerType = settingsAssembly.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager");
            var settingsManager = externalSettingsManagerType
                .GetMethods()
                .Where(x => x.Name == "CreateForApplication")
                .Where(x =>
                {
                    var parameters = x.GetParameters();
                    return
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(string);
                })
                .FirstOrDefault()
                .Invoke(null, new[] { applicationPath, rootSuffix });

            var extensionManagerServiceType = GetExtensionManagerServiceType(version);
            var extensionManager = (IVsExtensionManager)extensionManagerServiceType
                .GetConstructors()
                .Where(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.Name.Contains("SettingsManager"))
                .FirstOrDefault()
                .Invoke(new[] { settingsManager });
            return extensionManager;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RainMeadow
{
    static class MethodCache
    {
        public static Dictionary<Module, Assembly> cachedAssemblies = new Dictionary<Module, Assembly>();
        public static Dictionary<Module, TypeBuilder> buildingTypes = new Dictionary<Module, TypeBuilder>();

        // return false if cached. returns new MethodBase and false if not.
        public static bool LoadMethod(string key, Module connectedModule, out MethodBase method,
            Type returnType, params Type[] argumentTypes)
        {
            var version = connectedModule.ModuleVersionId;
            var assemblyfile = connectedModule.Name + version.ToString("N") + ".MeadowCache" + ".dll";

            if (!cachedAssemblies.ContainsKey(connectedModule) && !buildingTypes.ContainsKey(connectedModule))
            {
                if (File.Exists(assemblyfile))
                {
                    cachedAssemblies.Add(connectedModule, Assembly.Load(File.ReadAllBytes(assemblyfile)));
                }
                else
                {
                    var buildingassemb = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName() { Name = assemblyfile }, AssemblyBuilderAccess.RunAndSave);
                    var module = buildingassemb.DefineDynamicModule(assemblyfile, assemblyfile, false);
                    buildingTypes.Add(connectedModule, module.DefineType("Cache", TypeAttributes.Public | TypeAttributes.Sealed));
                }
            }

            if (cachedAssemblies.ContainsKey(connectedModule))
            {
                method = cachedAssemblies[connectedModule].DefinedTypes.First().DeclaredMethods.First(x => x.Name == key);
                return true;
            }

            method = buildingTypes[connectedModule].GetMethods().Where(x => x.Name == key).FirstOrDefault();
            if (method is null)
            {
                method = buildingTypes[connectedModule].DefineMethod(key, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, argumentTypes);
                return false;
            }

            return true;
        }

        public static void SaveDynamicMethods()
        {
            foreach (TypeBuilder type in buildingTypes.Values)
            {
                type.CreateType();
                ((AssemblyBuilder)type.Assembly).Save(type.Assembly.GetName().Name);
            }
        }
    }
}
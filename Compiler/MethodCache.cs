
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
        public static bool compiledCache = false;
        public static Dictionary<Module, Assembly> cachedAssemblies = new Dictionary<Module, Assembly>();
        public static Dictionary<Module, TypeBuilder> buildingTypes = new Dictionary<Module, TypeBuilder>();
        private static List<(MethodBase, Action<MethodInfo>)> compilationCallbacks = new();


        // return false if cached. returns new MethodBase and false if not.
        public static bool LoadMethod(string key, Module connectedModule, out MethodBase method, Action<MethodInfo> onCompletedCompilation,
            Type returnType, params Type[] argumentTypes)
        {
            var modCacheDirectory = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").basePath, "cache");
            if (Directory.Exists(modCacheDirectory)) Directory.CreateDirectory(modCacheDirectory);

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
                    var buildingassemb = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName() { Name = assemblyfile }, AssemblyBuilderAccess.RunAndSave, modCacheDirectory);
                    var module = buildingassemb.DefineDynamicModule(assemblyfile, assemblyfile, false);
                    buildingTypes.Add(connectedModule, module.DefineType("Cache", TypeAttributes.Public | TypeAttributes.Sealed));
                }
            }

            if (cachedAssemblies.ContainsKey(connectedModule))
            {
                method = cachedAssemblies[connectedModule].DefinedTypes.First().DeclaredMethods.First(x => x.Name == key);
                onCompletedCompilation((MethodInfo)method);
                return true;
            }

            method = buildingTypes[connectedModule].GetMethods().Where(x => x.Name == key).FirstOrDefault();
            if (method is null)
            {
                method = buildingTypes[connectedModule].DefineMethod(key, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, argumentTypes);
                compilationCallbacks.Add((method, onCompletedCompilation));
                return false;
            }

            return true;
        }

        public static void SaveDynamicMethods()
        {
            if (compiledCache) throw new InvalidOperationException("We already compiled the cache");
            foreach (TypeBuilder type in buildingTypes.Values)
            {
                var compiledType = type.CreateType();
                ((AssemblyBuilder)type.Assembly).Save(type.Assembly.GetName().Name);
                foreach ((MethodBase info, Action<MethodInfo> action) in compilationCallbacks)
                {
                    var compiledMethod = compiledType.GetMethod(
                        info.Name, BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Standard, info.GetParameters().Select(x => x.ParameterType).ToArray(), null
                    );

                    if (compiledMethod is null) throw new InvalidProgrammerException($"Could not find compiled method \"{info.Name}\" ");
                    action(compiledMethod);
                }

                compilationCallbacks.Clear();
            }
            compiledCache = true;
        }
    }
}
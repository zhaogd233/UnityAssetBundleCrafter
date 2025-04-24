using System.Reflection;
using Mono.Cecil;

namespace BundleCrafter
{
    public class CustomAssemblyResolver : BaseAssemblyResolver
    {
        private readonly string[] _searchDirectories;
        Dictionary<string, Assembly> asmDict = new Dictionary<string, Assembly>();

        Dictionary<string, AssemblyDefinition> loadedAsm = new Dictionary<string, AssemblyDefinition>();


        public CustomAssemblyResolver(params string[] searchDirectories)
        {
            _searchDirectories = searchDirectories;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            Assembly[] assemblies = currentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var asmName = asm.GetName().Name;
                if (asmDict.TryGetValue(asmName, out var asm2))
                {
                    Console.WriteLine($"Duplicate assembly name: {asmName}");
                }
                else
                {
                    asmDict.Add(asmName, asm);
                }
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            parameters.AssemblyResolver = this;

            if (loadedAsm.TryGetValue(name.Name, out var assembly))
            {
                return assembly;
            }

            var asm = SelfResolve(name, parameters);
            if (asm != null)
            {
                loadedAsm.Add(name.Name, asm);
                return asm;
            }

            asm = base.Resolve(name, parameters);
            if (asm != null)
            {
                loadedAsm.Add(name.Name, asm);
                return asm;
            }

            return null;
        }

        AssemblyDefinition SelfResolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            foreach (var directory in _searchDirectories)
            {
                var assemblyPath = System.IO.Path.Combine(directory, name.Name + ".dll");
                if (System.IO.File.Exists(assemblyPath))
                {
                    return AssemblyDefinition.ReadAssembly(assemblyPath, parameters);
                }
            }

            if (asmDict.TryGetValue(name.Name, out var asm))
            {
                var p = asm.Location;
                return AssemblyDefinition.ReadAssembly(p, parameters);
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var pair in loadedAsm)
            {
                pair.Value.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
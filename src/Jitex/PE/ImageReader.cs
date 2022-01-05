using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using System.Reflection;

namespace Jitex.PE
{
    internal class ImageReader : IDisposable
    {
        private static readonly IDictionary<Module, ImageInfo> ImageCache = new Dictionary<Module, ImageInfo>();

        private readonly Module _module;
        private ModuleContext? _moduleContext;
        private ModuleDefMD? _moduleDef;
        private ImageInfo? _image;

        public ImageReader(Module module)
        {
            _module = module;
        }

        public ImageInfo LoadImage()
        {
            if (!ImageCache.TryGetValue(_module, out _image))
            {
                _image = new ImageInfo(_module);
                ImageCache.Add(_module, _image);
                ReadImage();
            }

            return _image;
        }

        private void ReadImage()
        {
            _moduleContext = ModuleDef.CreateModuleContext();
            _moduleContext.AssemblyResolver = new CustomResolver();
            _moduleDef = ModuleDefMD.Load(_module, _moduleContext);

            IReadOnlyCollection<Module> modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(w => w.Modules).ToList();


            LoadMemberRefs(modules);
            LoadTypeRefs(modules);
        }

        private void LoadMemberRefs(IReadOnlyCollection<Module> modules)
        {
            IList<MemberRef> memberRefs = _moduleDef!.GetMemberRefs().ToList();
            _image!.NumberOfMemberRefRows = memberRefs.Count;

            foreach (MemberRef memberRef in memberRefs)
            {
                if (!memberRef.IsMethodRef) continue;

                MethodDef? methodDef = memberRef.ResolveMethod();

                if (methodDef == null) continue;

                Module? module = modules.FirstOrDefault(w => w.FullyQualifiedName == methodDef.Module.Location);

                if (module == null)
                    continue;

                try
                {
                    MethodBase method = module.ResolveMethod((int) methodDef.MDToken.Raw);
                    _image!.AddMemberRefFromImage(method, (int) memberRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        private void LoadTypeRefs(IReadOnlyCollection<Module> modules)
        {
            IList<TypeRef> typeRefs = _moduleDef!.GetTypeRefs().ToList();
            _image!.NumberOfTypeRefRows = typeRefs.Count;

            foreach (TypeRef typeRef in typeRefs)
            {
                TypeDef? typeDef = typeRef.Resolve();

                if (typeDef == null) continue;

                Module? module = modules.FirstOrDefault(w => w.FullyQualifiedName == typeDef.Module.Location);

                if (module == null)
                    continue;

                try
                {
                    Type type = module.ResolveType((int) typeDef.MDToken.Raw);
                    _image!.AddTypeRefFromImage(type, (int) typeRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        public void Dispose()
        {
            _moduleDef?.Dispose();
        }

        private class CustomResolver : AssemblyResolver
        {
            private readonly string _dotnetPath;

            public CustomResolver()
            {
                _dotnetPath = Path.GetDirectoryName(typeof(void).Assembly.Location)!;
            }

            protected override IEnumerable<string> FindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly)
            {
                IEnumerable<string> paths = base.FindAssemblies(assembly, sourceModule, matchExactly);
                string dllPath = Path.Combine(_dotnetPath, assembly.Name + ".dll");
                yield return dllPath;

                foreach (string path in paths)
                {
                    if (path == dllPath)
                        continue;

                    yield return path;
                }
            }
        }
    }
}
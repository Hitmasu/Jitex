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
            LoadMethodRefs(modules);
            LoadTypeRefs(modules);
        }

        private void LoadMethodRefs(IReadOnlyCollection<Module> modules)
        {
            foreach (MemberRef memberRef in _moduleDef!.GetMemberRefs())
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
                    _image!.AddNewMethodRef(method, (int) memberRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        private void LoadTypeRefs(IReadOnlyCollection<Module> modules)
        {
            foreach (TypeRef typeRef in _moduleDef!.GetTypeRefs())
            {
                TypeDef? typeDef = typeRef.Resolve();

                if (typeDef == null) continue;

                Module? module = modules.FirstOrDefault(w => w.FullyQualifiedName == typeDef.Module.Location);

                if (module == null)
                    continue;

                try
                {
                    Type type = module.ResolveType((int) typeDef.MDToken.Raw);
                    _image!.AddNewTypeRef(type, (int) typeRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        public bool TryGetRefToken(MethodBase method, out int refMetadataToken)
        {
            foreach (MemberRef memberRef in _moduleDef.GetMemberRefs())
            {
                if (!memberRef.IsMethodRef) continue;

                MethodDef methodRef = memberRef.ResolveMethod();

                if (methodRef == null) continue;

                if (methodRef.MDToken.Raw == method.MetadataToken && methodRef.Module.Location == method.Module.FullyQualifiedName)
                {
                    refMetadataToken = (int) memberRef.MDToken.Raw;
                    return true;
                }
            }

            refMetadataToken = 0;
            return false;
        }

        public void Dispose()
        {
            _moduleDef.Dispose();
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using System.Reflection;
using Jitex.Utils;

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

        public ImageInfo LoadImage(bool reuseReferences)
        {
            if (!ImageCache.TryGetValue(_module, out _image))
            {
                _image = new ImageInfo(_module);
                ImageCache.Add(_module, _image);
                ReadImage(reuseReferences);
            }

            return _image;
        }

        private void ReadImage(bool readReferences)
        {
            _moduleContext = ModuleDef.CreateModuleContext();
            _moduleContext.AssemblyResolver = new CustomResolver();
            _moduleDef = ModuleDefMD.Load(_module, _moduleContext);

            IReadOnlyCollection<Module> modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(w => w.Modules).ToList();
            IDictionary<string, Module> dicModules = new Dictionary<string, Module>();

            foreach (Module module in modules)
            {
                if (!dicModules.ContainsKey(module.FullyQualifiedName))
                    dicModules.Add(module.FullyQualifiedName, module);
            }

            if (readReferences)
            {
                LoadMemberRefs(dicModules);
                LoadTypeRefs(dicModules);
                LoadMethodSpecs(dicModules);
            }
            else
            {
                _image!.NumberOfMemberRefRows = (int) _moduleDef.TablesStream.MemberRefTable.Rows;
                _image!.NumberOfTypeRefRows = (int) _moduleDef.TablesStream.TypeRefTable.Rows;
                _image!.NumberOfMethodSpecRows = (int) _moduleDef!.TablesStream.MethodSpecTable.Rows;
            }
        }

        private void LoadMemberRefs(IDictionary<string, Module> modules)
        {
            IList<MemberRef> memberRefs = _moduleDef!.GetMemberRefs().ToList();
            _image!.NumberOfMemberRefRows = memberRefs.Count;

            foreach (MemberRef memberRef in memberRefs)
            {
                if (!memberRef.IsMethodRef) continue;

                Module? module = GetModule(modules, memberRef.Module.Location, _module.Name);

                if (module == null)
                    continue;

                try
                {
                    MethodBase method = module.ResolveMethod((int) memberRef.MDToken.Raw);
                    _image!.AddMemberRefFromImage(method, (int) memberRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        private void LoadTypeRefs(IDictionary<string, Module> modules)
        {
            IList<TypeRef> typeRefs = _moduleDef!.GetTypeRefs().ToList();
            _image!.NumberOfTypeRefRows = typeRefs.Count;

            foreach (TypeRef typeRef in typeRefs)
            {
                Module? module = GetModule(modules, typeRef.Module.Location, typeRef.Module.Name);

                if (module == null)
                    continue;

                try
                {
                    Type type = module.ResolveType((int) typeRef.MDToken.Raw);
                    _image!.AddTypeRefFromImage(type, (int) typeRef.MDToken.Raw);
                }
                catch
                {
                    //Just ignore
                }
            }
        }

        private void LoadMethodSpecs(IDictionary<string, Module> modules)
        {
            _image!.NumberOfMethodSpecRows = (int) _moduleDef!.TablesStream.MethodSpecTable.Rows;

            for (int i = 0; i < _image.NumberOfMethodSpecRows; i++)
            {
                MethodSpec methodSpec = _moduleDef.ResolveMethodSpec((uint) (i + 1));

                Module? module = GetModule(modules, methodSpec.Module.Location, methodSpec.Module.Name);

                if (module == null)
                    continue;

                Type declaringType = ResolveTypeGeneric(modules, methodSpec.DeclaringType);

                Type[] genericArguments = new Type[methodSpec.GenericInstMethodSig.GenericArguments.Count];

                bool failedResolve = false;

                for (int j = 0; j < genericArguments.Length; j++)
                {
                    ITypeDefOrRef genericArgument = methodSpec.GenericInstMethodSig.GenericArguments[j].ToTypeDefOrRef();
                    Type? typeResolved = ResolveTypeGeneric(modules, genericArgument);

                    if (typeResolved == null)
                    {
                        failedResolve = true;
                        break;
                    }

                    genericArguments[j] = typeResolved;
                }

                if (failedResolve)
                    continue;

                MethodInfo methodInfo = (MethodInfo) module.ResolveMethod((int) methodSpec.MDToken.Raw, declaringType.GetGenericArguments(), genericArguments);
                _image.AddMethodSpecFromImage(methodInfo, (int) methodSpec.MDToken.Raw);
            }
        }


        private Type? ResolveTypeGeneric(IDictionary<string, Module> modules, ITypeDefOrRef typeDefOrRef)
        {
            if (typeDefOrRef.Module == null)
                return null;

            if (typeDefOrRef.IsTypeRef)
            {
                Type? typeRef = _image!.GetTypeRef((int) typeDefOrRef.MDToken.Raw);

                if (typeRef != null)
                    return typeRef;

                typeDefOrRef = typeDefOrRef.ResolveTypeDef();
            }

            Type[] genericArguments = new Type[typeDefOrRef.NumberOfGenericParameters];

            if (genericArguments.Length > 0)
            {
                IList<TypeSig> sigs = typeDefOrRef.TryGetGenericInstSig().GenericArguments;
                typeDefOrRef = typeDefOrRef.TryGetGenericInstSig().GenericType.ToTypeDefOrRef();
                for (int i = 0; i < sigs.Count; i++)
                {
                    TypeSig genericArgument = sigs[i];
                    ITypeDefOrRef? genericTypeDefOrRef = genericArgument.ToTypeDefOrRef();
                    genericArguments[i] = ResolveTypeGeneric(modules, genericTypeDefOrRef);
                }
            }

            Module? module = GetModule(modules, typeDefOrRef.Module.Location, typeDefOrRef.Module.Name);

            if (module == null)
                return null;

            Type type = module.ResolveType((int) typeDefOrRef.MDToken.Raw);

            if (genericArguments.Length > 0)
                type = type.MakeGenericType(genericArguments);

            return type;
        }

        private Module? GetModule(IDictionary<string, Module> modules, string location, string moduleName)
        {
            if (modules.TryGetValue(location, out Module module))
                return module;

            return moduleName == _module.Name ? _module : null;
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
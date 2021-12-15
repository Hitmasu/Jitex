using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using System.Reflection;

namespace Jitex.PE
{
    public class ImageReader : IDisposable
    {
        private readonly Module _module;
        private ModuleContext _moduleContext;
        private ModuleDefMD _moduleDef;
        private ImageInfo _image;

        public ImageReader(Module module)
        {
            _module = module;
        }

        public ImageInfo LoadImage()
        {
            _moduleContext = ModuleDef.CreateModuleContext();
            _moduleContext.AssemblyResolver = new CustomResolver();
            _moduleDef = ModuleDefMD.Load(_module, _moduleContext);

            _image = new ImageInfo(_module)
            {
                MethodRefRows = (int) _moduleDef.Metadata.TablesStream.MemberRefTable.Rows
            };

            return _image;
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
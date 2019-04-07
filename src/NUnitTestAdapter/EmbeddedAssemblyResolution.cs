using System;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NET35
using System.Runtime.Loader;
#endif

namespace NUnit.VisualStudio.TestAdapter
{
    internal static class EmbeddedAssemblyResolution
    {
        private static readonly object lockObj = new object();
        private static bool isInitialized;

        public static void EnsureInitialized()
        {
            if (isInitialized) return;

            lock (lockObj)
            {
                if (isInitialized) return;

#if NET35
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#else
                AssemblyLoadContext.Default.Resolving += Default_Resolving;
#endif

                isInitialized = true;
            }
        }

#if NET35
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            using (var stream = TryGetResourceAssemblyStream(new AssemblyName(args.Name)))
            {
                return stream == null ? null : Assembly.Load(stream.ReadToEnd());
            }
        }
#else
        private static Assembly Default_Resolving(AssemblyLoadContext context, AssemblyName name)
        {
            using (var stream = TryGetResourceAssemblyStream(name))
            {
                if (stream == null) return null;
                return context.LoadFromStream(stream);
            }
        }
#endif

        private static readonly string[] AllowedResourceAssemblyNames =
        {
            "Mono.Cecil"
        };

        private static Stream TryGetResourceAssemblyStream(AssemblyName assemblyName)
        {
            var properCasing = AllowedResourceAssemblyNames.FirstOrDefault(name =>
                name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));

            if (properCasing == null) return null;

            return typeof(EmbeddedAssemblyResolution)
#if !NET35
                .GetTypeInfo()
#endif
                .Assembly.GetManifestResourceStream(@"Assemblies\" + properCasing + ".dll");
        }

#if NET35
        private static byte[] ReadToEnd(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));
            if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable.", nameof(stream));

            var array = new byte[stream.Length - stream.Position];

            var position = 0;
            for (int bytesRead; position < array.Length; position += bytesRead)
            {
                bytesRead = stream.Read(array, position, array.Length - position);
                if (bytesRead == 0) break;
            }

            if (position == array.Length) return array;

            var resized = new byte[position];
            Array.Copy(array, resized, position);
            return resized;
        }
#endif
    }
}

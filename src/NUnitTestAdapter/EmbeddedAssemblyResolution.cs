// ***********************************************************************
// Copyright (c) 2014-2021 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************


#if ENABLED
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
#if !NET462
using System.Runtime.Loader;
#endif

namespace NUnit.VisualStudio.TestAdapter
{
    internal static class EmbeddedAssemblyResolution
    {
        [ModuleInitializer]
        public static void EnsureInitialized()
        {
#if NET462
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#else
            AssemblyLoadContext.Default.Resolving += Default_Resolving;
#endif
        }

#if NET462
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
#if !NET462
                .GetTypeInfo()
#endif
                .Assembly.GetManifestResourceStream(@"Assemblies/" + properCasing + ".dll");
        }

#if NET462
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
#endif

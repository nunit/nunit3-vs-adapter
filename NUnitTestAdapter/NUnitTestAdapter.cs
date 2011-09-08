// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Util;
using AssemblyHelper = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.AssemblyHelper;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        #region Constructor

        /// <summary>
        /// The common constructor initializes NUnit services 
        /// needed to load and run tests.
        /// </summary>
        public NUnitTestAdapter()
        {
            ServiceManager.Services.AddService(new DomainManager());
            ServiceManager.Services.AddService(new ProjectService());

            ServiceManager.Services.InitializeServices();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sanitize the parameter sources and discard the ones which cannot have NUnit test cases.  
        /// </summary>
        public static List<string> SanitizeSources(IEnumerable<string> sources)
        {
            List<string> result = new List<string>();

            foreach (string source in sources)
            {
                if (CanHaveNUnitFrameworkReference(source))
                {
                    result.Add(source);
                }
            }

            return result;
        }


        /// <summary>
        /// Returns whether the parameter source can have NUnit Framework dll reference
        /// </summary>
        public static bool CanHaveNUnitFrameworkReference(string source)
        {
            try
            {
                string[] referencedAssemblies = AssemblyHelper.GetReferencedAssemblies(source);

                // GetReferencedAssemblies API returns null on error, so this means that we couldnot infer
                // whether the dll contains NUnit framework dll reference or not which means that it 
                // can have NUnit test cases.
                // 
                if (referencedAssemblies == null)
                {
                    return true;
                }


                return referencedAssemblies.Any(
                                assemblyName =>
                                   (
                                    !string.IsNullOrEmpty(assemblyName)
                                      &&
                                     assemblyName.StartsWith("NUnit.Framework",
                                                                    StringComparison.OrdinalIgnoreCase)
                                   ));
            }
            catch (Exception)
            {
                return true;
            }
        }

        #endregion
    }
}

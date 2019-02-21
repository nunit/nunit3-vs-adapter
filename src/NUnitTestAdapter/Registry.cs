#if NET35
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace NUnit.VisualStudio.TestAdapter
{
    public abstract class RegistryBase
    {

        private string SubKeyName { get; set; }
        protected string BaseKey { get; private set; }

        protected RegistryBase(string basekey, string subkeyname)
        {

            BaseKey = basekey; // RegistryKey.OpenBaseKey(basekey, RegistryView.Default);
            SubKeyName = subkeyname;
        }

        public T Read<T>(string property)
        {
            var result = Registry.GetValue(BaseKey + SubKeyName, property, null);
            if (result == null)
                return default(T);
            var value = (T)result;
            return value;
        }

        public bool Exist(string property)
        {
            var value = Registry.GetValue(BaseKey + SubKeyName, property,null);
            return value != null;

        }

        public void Write<T>(string property, T val)
        {
            Registry.SetValue(BaseKey+SubKeyName,property, val);
        }
    }

    public class RegistryCurrentUser : RegistryBase
    {
        public RegistryCurrentUser(string subkeyname)
            : base(@"HKEY_CURRENT_USER\", subkeyname)
        {

        }

        private static RegistryCurrentUser currentUser;

        public static RegistryCurrentUser OpenRegistryCurrentUser(string key)
        {
            return currentUser ?? (currentUser = new RegistryCurrentUser(key));
        }

        /// <summary>
        /// Used for test mocking only
        /// </summary>
        /// <param name="user"></param>
        public void SetCurrentUser(RegistryCurrentUser user)
        {
            currentUser = user;
        }
    }
}
#endif
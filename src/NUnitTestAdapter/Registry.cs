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
        protected RegistryKey BaseKey { get; private set; }

        protected RegistryBase(RegistryHive basekey, string subkeyname)
        {
            
            BaseKey = RegistryKey.OpenBaseKey(basekey, RegistryView.Default);
            SubKeyName = subkeyname;
        }

        public T Read<T>(string property)
        {
            var key = BaseKey.OpenSubKey(SubKeyName);
            if (key == null)
                return default(T);
            var o = key.GetValue(property);
            return (T)o;
        }

        public bool Exist
        {
            get
            {
                return BaseKey.OpenSubKey(SubKeyName) != null;
            }
        }

        public void Write<T>(string property, T val)
        {
            var key = BaseKey.OpenSubKey(SubKeyName, true) ?? BaseKey.CreateSubKey(SubKeyName);
            key.SetValue(property, val);
            key.Close();
        }
    }

    public class RegistryCurrentUser : RegistryBase
    {
        public RegistryCurrentUser(string subkeyname)
            : base(RegistryHive.CurrentUser, subkeyname)
        {

        }

        private static RegistryCurrentUser currentUser;

        public static RegistryCurrentUser CreateRegistryCurrentUser(string key)
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



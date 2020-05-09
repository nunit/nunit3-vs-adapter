using System;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// Need this just because we're still on .net 3.5 and it don't have TryParse :-(.
    /// </summary>
    public static class TryParse
    {
        public static bool EnumTryParse<T>(string input, out T result)
            where T : Enum
        {
            result = default;
            if (string.IsNullOrEmpty(input))
                return false;
            try
            {
                result = (T)Enum.Parse(typeof(T), input, true);
                return Enum.IsDefined(typeof(T), result);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
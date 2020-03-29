using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public interface IChangeTrackingContext
    {

        void SetContextFor<T>(string context) where T : class;

        string GetContextFor<T>() where T : class;
    }

    public class ChangeTrackingContext : IChangeTrackingContext
    {
        Dictionary<Type, string> _contextDictionary = new Dictionary<Type, string>();

        public void SetContextFor<T>(string context) where T : class
        {
            if (!_contextDictionary.ContainsKey(typeof(T)))
                _contextDictionary.Add(typeof(T), context);
        }

        public string GetContextFor<T>() where T : class
        {
            _contextDictionary.TryGetValue(typeof(T), out string value);

            return value;
        }
    }
}

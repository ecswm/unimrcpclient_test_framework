using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
//using System.Web.Script.Serialization;
using System.Runtime.CompilerServices;

namespace AMC
{
    public class AMCBundle
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        //private JavaScriptSerializer _serializer = new JavaScriptSerializer();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void put(string key, object value)
        {
            if (key != null && value != null && value.GetType().IsSerializable)
            {
                _values[key] = value;
            }            
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public T get<T>(string key, T def)
        {
            if (_values.ContainsKey(key))
            {
                return (T)_values[key];
            }
            return def;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal byte[] Serialize()
        {
            //string s = _serializer.Serialize(_values);
            //return UTF8Encoding.UTF8.GetBytes(s);
            return new byte[0];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Deserialize(byte[] data)
        {
            //string s = UTF8Encoding.UTF8.GetString(data);
            //_values = (Dictionary<string, object>)_serializer.Deserialize(s, _values.GetType());
        }
    }
}

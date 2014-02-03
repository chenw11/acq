using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Lab.IO
{
    public static class CLRSerial
    {
        public static void Serialize<T>(T x, Stream s)
        {
            var bf = new BinaryFormatter();
            bf.Serialize(s, x);
        }

        public static T Deserialize<T>(Stream s)
        {
            var bf = new BinaryFormatter();
            return (T)(bf.Deserialize(s));
        }

        public static void Serialize<T>(T x, string path)
        {
            using (var fs = File.Create(path))
            {
                Serialize<T>(x, fs);
                fs.Flush();
            }
        }

        public static T Deserialize<T>(string path)
        {
            using (var fs = File.OpenRead(path))
                return Deserialize<T>(fs);
        }
    }
}

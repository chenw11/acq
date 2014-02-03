using System;
using System.IO;
using ProtoBuf.Meta;

namespace Lab
{
    public class ProtoBufByteConverter
    {
        readonly MemoryStream ms = new MemoryStream();
        public TypeModel CustomSerializer { get; set; }

        public ProtoBufByteConverter(TypeModel customSerializer)
        {
            this.CustomSerializer = customSerializer;
        }

        public ProtoBufByteConverter() : this(null) { }

        public byte[] ToBytes(object o)
        {
            if (o == null)
                throw new ArgumentNullException();
            lock (ms)
            {
                ms.Seek(0, SeekOrigin.Begin);
                CustomSerializer.NullSafe_Serialize(ms, o);
                ms.SetLength(ms.Position);
                return ms.ToArray();
            }
        }

        public object FromBytes(byte[] bytes, Type objType)
        {
            if ((bytes == null) || (objType == null))
                throw new ArgumentNullException();
            lock (ms)
            {
                ms.Seek(0, SeekOrigin.Begin);
                ms.Write(bytes, 0, bytes.Length);
                ms.SetLength(ms.Position);
                ms.Seek(0, SeekOrigin.Begin);
                return CustomSerializer.NullSafe_Deserialize(ms, objType);
            }
        }

        public T FromBytes<T>(byte[] bytes)
        {
            return (T)FromBytes(bytes, typeof(T));
        }
    }

}

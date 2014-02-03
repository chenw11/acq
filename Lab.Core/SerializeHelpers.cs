using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;


namespace Lab
{
    public static class SerializeHelpers
    {
        public static void NullSafe_Serialize_Int32Prefix(this TypeModel customSerializer,
            Stream stream, object toSerialize, Type objectType)
        {
            if (customSerializer == null)
                Serializer.NonGeneric.SerializeWithLengthPrefix(stream, toSerialize, PrefixStyle.Fixed32, 0);
            else
                customSerializer.SerializeWithLengthPrefix(stream, toSerialize, objectType, PrefixStyle.Fixed32, 0);
        }

        public static void NullSafe_Serialize_Int32Prefix<T>(this TypeModel customSerializer,
            Stream stream, T toSerialize)
        {
            NullSafe_Serialize_Int32Prefix(customSerializer, stream, toSerialize, typeof(T));
        }


        public static T NullSafe_Deserialize_Int32Prefix<T>(this TypeModel customSerializer, Stream stream)
        {
            if (customSerializer == null)
                return Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Fixed32);
            else
                return (T)customSerializer.DeserializeWithLengthPrefix(stream, null, typeof(T), PrefixStyle.Fixed32, 0);
        }


        public static T NullSafe_Deserialize<T>(this TypeModel customSerializer, Stream stream)
        {
            if (customSerializer == null)
                return Serializer.Deserialize<T>(stream);
            else
                return (T)(customSerializer.Deserialize(stream, null, typeof(T)));
        }

        public static object NullSafe_Deserialize(this TypeModel customSerializer, Stream stream, Type objectType)
        {
            if (customSerializer == null)
                return Serializer.NonGeneric.Deserialize(objectType, stream);
            else
                return customSerializer.Deserialize(stream, null, objectType);
        }

        public static void NullSafe_Serialize(this TypeModel customSerializer, Stream stream, object toSerialize)
        {
            if (customSerializer == null)
                Serializer.NonGeneric.Serialize(stream, toSerialize);
            else
                customSerializer.Serialize(stream, toSerialize);
        }

    }
}


using Newtonsoft.Json;
using System;
using Unity.Mathematics;
using Theblueway.Core.Runtime;
using UnityEngine;


namespace Assets._Project.Scripts.Core.Serializers.Json_
{

    public class Float3JsonConverter : JsonConverter<float3>
    {
        public override float3 ReadJson(JsonReader reader, Type objectType, float3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var vector = new float3();

            reader.Read(); //property name
            vector.x = reader.ReadSingle();
            reader.Read();
            vector.y = reader.ReadSingle();
            reader.Read();
            vector.z = reader.ReadSingle();

            reader.Read(); //end object

            return vector;
        }

        public override void WriteJson(JsonWriter writer, float3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }
    }


    public class RectJsonConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var rect = new Rect();

            reader.Read(); //property name
            rect.x = reader.ReadSingle();
            reader.Read();
            rect.y = reader.ReadSingle();
            reader.Read();
            rect.width = reader.ReadSingle();
            reader.Read();
            rect.height = reader.ReadSingle();

            reader.Read(); //end object

            return rect;
        }
    }

    //public class LayerMaskJsonConverter : JsonConverter<LayerMask>
    //{
    //    public override LayerMask ReadJson(JsonReader reader, Type objectType, LayerMask existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    {
    //        //reader.Read();
    //        existingValue = reader.ReadAsInt32().Value;
    //        return existingValue;
    //    }

    //    public override void WriteJson(JsonWriter writer, LayerMask value, JsonSerializer serializer)
    //    {
    //        writer.WriteValue((int)value);
    //    }
    //}


    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var color = new Color
            {
                r = reader.ReadSingle(),
                g = reader.ReadSingle(),
                b = reader.ReadSingle(),
                a = reader.ReadSingle()
            };
            reader.Read(); // Read the end of the array

            return color;
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.r);
            writer.WriteValue(value.g);
            writer.WriteValue(value.b);
            writer.WriteValue(value.a);
            writer.WriteEndArray();
        }
    }


    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var vector = new Vector3();

            reader.Read(); //property name
            vector.x = reader.ReadSingle();
            reader.Read();
            vector.y = reader.ReadSingle();
            reader.Read();
            vector.z = reader.ReadSingle();

            reader.Read(); //end object

            return vector;
        }
    }
    public class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var quaternion = new Quaternion();

            reader.Read(); //property name
            quaternion.x = reader.ReadSingle();
            reader.Read();
            quaternion.y = reader.ReadSingle();
            reader.Read();
            quaternion.z = reader.ReadSingle();
            reader.Read();
            quaternion.w = reader.ReadSingle();

            reader.Read(); //end object

            return quaternion;
        }
    }
    public class Vector4JsonConverter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var vector = new Vector4();

            reader.Read(); //property name
            vector.x = reader.ReadSingle();
            reader.Read();
            vector.y = reader.ReadSingle();
            reader.Read();
            vector.z = reader.ReadSingle();
            reader.Read();
            vector.w = reader.ReadSingle();

            reader.Read(); //end object

            return vector;
        }
    }
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var vector = new Vector2();

            reader.Read(); //property name
            vector.x = reader.ReadSingle();
            reader.Read();
            vector.y = reader.ReadSingle();

            reader.Read(); //end object

            return vector;
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }
    }


    public class Vector2IntJsonConverter : JsonConverter<Vector2Int>
    {
        public override Vector2Int ReadJson(JsonReader reader, Type objectType, Vector2Int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var vector = new Vector2Int();

            reader.Read(); //property name
            vector.x = reader.ReadAsInt32().Value;
            reader.Read();
            vector.y = reader.ReadAsInt32().Value;

            reader.Read(); //end object

            return vector;
        }

        public override void WriteJson(JsonWriter writer, Vector2Int value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }
    }


    public class Hash128JsonConverter : JsonConverter<Hash128>
    {
        public override Hash128 ReadJson(JsonReader reader, Type objectType, Hash128 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Hash128.Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, Hash128 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }


    public class Matrix4x4JsonConverter : JsonConverter<Matrix4x4>
    {
        public override void WriteJson(JsonWriter writer, Matrix4x4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("m00"); writer.WriteValue(value.m00);
            writer.WritePropertyName("m01"); writer.WriteValue(value.m01);
            writer.WritePropertyName("m02"); writer.WriteValue(value.m02);
            writer.WritePropertyName("m03"); writer.WriteValue(value.m03);
            writer.WritePropertyName("m10"); writer.WriteValue(value.m10);
            writer.WritePropertyName("m11"); writer.WriteValue(value.m11);
            writer.WritePropertyName("m12"); writer.WriteValue(value.m12);
            writer.WritePropertyName("m13"); writer.WriteValue(value.m13);
            writer.WritePropertyName("m20"); writer.WriteValue(value.m20);
            writer.WritePropertyName("m21"); writer.WriteValue(value.m21);
            writer.WritePropertyName("m22"); writer.WriteValue(value.m22);
            writer.WritePropertyName("m23"); writer.WriteValue(value.m23);
            writer.WritePropertyName("m30"); writer.WriteValue(value.m30);
            writer.WritePropertyName("m31"); writer.WriteValue(value.m31);
            writer.WritePropertyName("m32"); writer.WriteValue(value.m32);
            writer.WritePropertyName("m33"); writer.WriteValue(value.m33);

            writer.WriteEndObject();
        }

        public override Matrix4x4 ReadJson(JsonReader reader, System.Type objectType, Matrix4x4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var m = new Matrix4x4();

            reader.Read(); m.m00 = reader.ReadSingle();
            reader.Read(); m.m01 = reader.ReadSingle();
            reader.Read(); m.m02 = reader.ReadSingle();
            reader.Read(); m.m03 = reader.ReadSingle();
            reader.Read(); m.m10 = reader.ReadSingle();
            reader.Read(); m.m11 = reader.ReadSingle();
            reader.Read(); m.m12 = reader.ReadSingle();
            reader.Read(); m.m13 = reader.ReadSingle();
            reader.Read(); m.m20 = reader.ReadSingle();
            reader.Read(); m.m21 = reader.ReadSingle();
            reader.Read(); m.m22 = reader.ReadSingle();
            reader.Read(); m.m23 = reader.ReadSingle();
            reader.Read(); m.m30 = reader.ReadSingle();
            reader.Read(); m.m31 = reader.ReadSingle();
            reader.Read(); m.m32 = reader.ReadSingle();
            reader.Read(); m.m33 = reader.ReadSingle();

            reader.Read(); // end object

            return m;
        }
    }


}

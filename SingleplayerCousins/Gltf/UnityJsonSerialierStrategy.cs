using SimpleJson;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SingleplayerCousins.Gltf;

[AttributeUsage(AttributeTargets.Enum)]
internal sealed class StringEnumAttribute : Attribute { }

public sealed class UnityJsonSerializerStrategy : PocoJsonSerializerStrategy {
    public static void Register() {
        SimpleJson.SimpleJson.CurrentJsonSerializerStrategy = new UnityJsonSerializerStrategy();
    }

    public override bool TrySerializeNonPrimitiveObject(object input, out object output) {
        switch (input) {
            case Vector2 v: output = new[] { v.x, v.y }; return true;
            case Vector3 v: output = new[] { v.x, v.y, v.z }; return true;
            case Vector4 v: output = new[] { v.x, v.y, v.z, v.w }; return true;
            case Quaternion q: output = new[] { q.x, q.y, q.z, q.w }; return true;
            case Vector2Int v: output = new[] { v.x, v.y }; return true;
            case Vector3Int v: output = new[] { v.x, v.y, v.z }; return true;
            case Matrix4x4 m:
                output = new[] {
                    m.m00, m.m10, m.m20, m.m30,
                    m.m10, m.m11, m.m12, m.m13,
                    m.m20, m.m21, m.m22, m.m23,
                    m.m30, m.m31, m.m32, m.m33,
                };
                return true;
            case Enum e:
                break;
        }

        if (input.GetType().GetCustomAttributes(typeof(StringEnumAttribute), false).Length > 0) {
            output = input.ToString();
            return true;
        }

        return base.TrySerializeNonPrimitiveObject(input, out output);
    }

    private static readonly HashSet<Type>
        geometricFloat = [typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Matrix4x4)],
        geometricInt = [typeof(Vector2Int), typeof(Vector3Int)];

    public override object DeserializeObject(object value, Type type) {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (geometricFloat.Contains(type)) {
            var array = this.DeserializeObject<float[]>(value);
            if (array == null) return null!;
            return DeserializeGeometric(array, type);
        } else if (geometricInt.Contains(type)) {
            var array = this.DeserializeObject<int[]>(value);
            if (array == null) return null!;
            return DeserializeGeometric(array, type);
        } else if (type.IsEnum) {
            if (value is string s) {
                return Enum.Parse(type, s);
            } else {
                return Enum.ToObject(type, value);
            }
        } else {
            return base.DeserializeObject(value, type);
        }
    }

    private T DeserializeObject<T>(object value) => (T)base.DeserializeObject(value, typeof(T));

    private static object DeserializeGeometric(float[] a, Type t) {
        if (t == typeof(Vector2)) return new Vector2(a[0], a[1]);
        if (t == typeof(Vector3)) return new Vector3(a[0], a[1], a[2]);
        if (t == typeof(Vector4)) return new Vector4(a[0], a[1], a[2], a[3]);
        if (t == typeof(Quaternion)) return new Quaternion(a[0], a[1], a[2], a[3]);
        if (t == typeof(Matrix4x4)) return new Matrix4x4(
            new(a[0x0], a[0x1], a[0x2], a[0x3]),
            new(a[0x4], a[0x5], a[0x6], a[0x7]),
            new(a[0x8], a[0x9], a[0xa], a[0xb]),
            new(a[0xc], a[0xd], a[0xe], a[0xf])
        );
        return null!;
    }

    private static object DeserializeGeometric(int[] a, Type t) {
        if (t == typeof(Vector2Int)) return new Vector2Int(a[0], a[1]);
        if (t == typeof(Vector3Int)) return new Vector3Int(a[0], a[1], a[2]);
        return null!;
    }
}
using SimpleJson;

using System;

using UnityEngine;

namespace SingleplayerCousins.Gltf;

public sealed class UnityJsonSerializerStrategy : PocoJsonSerializerStrategy {
    public static void Register() {
        SimpleJson.SimpleJson.CurrentJsonSerializerStrategy = new UnityJsonSerializerStrategy();
    }

    public override bool TrySerializeNonPrimitiveObject(object input, out object output) {
        if (input is Vector3 v) {
            output = new[] { v.x, v.y, v.z };
            return true;
        } else if (input is Quaternion q) {
            output = new[] { q.x, q.y, q.z, q.w };
            return true;
        } else {
            return base.TrySerializeNonPrimitiveObject(input, out output);
        }
    }

    public override object DeserializeObject(object value, Type type) {
        if (type == typeof(Vector3)) {
            var xyz = (float[])base.DeserializeObject(value, typeof(float[]));
            return new Vector3(xyz[0], xyz[1], xyz[2]);
        } else if (type == typeof(Vector3?)) {
            var xyz = (float[]?)base.DeserializeObject(value, typeof(float[]));
            if (xyz == null) return null!;
            return (Vector3?)new Vector3(xyz[0], xyz[1], xyz[2]);
        } else if (type == typeof(Quaternion)) {
            var xyz = (float[])base.DeserializeObject(value, typeof(float[]));
            return new Vector3(xyz[0], xyz[1], xyz[2]);
        } else {
            return base.DeserializeObject(value, type);
        }
    }
}
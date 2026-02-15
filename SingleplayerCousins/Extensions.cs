using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins;

internal static class Extensions {
    public static GameObject[] GetChildren(this GameObject parent) {
        var ret = new GameObject[parent.transform.childCount];
        for (var i = 0; i < ret.Length; i++) {
            ret[i] = parent.transform.GetChild(i).gameObject;
        }
        return ret;
    }

    public static T? FirstWithName<T>(this IEnumerable<T> objects, string name) where T : Object {
        foreach (var obj in objects) if (obj?.name == name) return obj;
        return null;
    }

    public static List<T> AllWithNames<T>(this IEnumerable<T> objects, params string[] names) where T : Object {
        var list = new List<T>();
        foreach (var obj in objects) {
            if (obj != null && names.Contains(obj.name)) {
                list.Add(obj);
            }
        }
        return list;
    }

    public static Vector3 ToUnity(this Assimp.Vector3D v) => new(v.X, v.Y, v.Z);
    public static Vector2 ToUnityVec2(this Assimp.Vector3D v) => new(v.X, v.Y);
    public static Matrix4x4 ToUnity(this Assimp.Matrix4x4 v) => new(new(v.A1, v.B1, v.C1, v.D1), new(v.A2, v.B2, v.C2, v.D2), new(v.A3, v.B3, v.C3, v.D3), new(v.A4, v.B4, v.C4, v.D4));
}

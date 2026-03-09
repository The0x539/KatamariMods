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

    public static T? FirstWithName<T>(this IEnumerable<T> objects, string name) where T : UnityObject {
        foreach (var obj in objects) if (obj?.name == name) return obj;
        return null;
    }

    public static List<T> AllWithNames<T>(this IEnumerable<T> objects, params string[] names) where T : UnityObject {
        var list = new List<T>();
        foreach (var obj in objects) {
            if (obj != null && names.Contains(obj.name)) {
                list.Add(obj);
            }
        }
        return list;
    }

    public static int IndexOf<T>(this T[] objects, string name) where T : UnityObject {
        for (var i = 0; i < objects.Length; i++) {
            if (objects[i].name == name) {
                return i;
            }
        }
        throw new System.InvalidOperationException($"No object with name {name} in list");
    }

    public static Vector3 ToUnity(this Assimp.Vector3D v) => new(v.X, v.Y, v.Z);
    public static Vector2 ToUnityVec2(this Assimp.Vector3D v) => new(v.X, v.Y);
    public static Matrix4x4 ToUnity(this Assimp.Matrix4x4 v) => new(new(v.A1, v.B1, v.C1, v.D1), new(v.A2, v.B2, v.C2, v.D2), new(v.A3, v.B3, v.C3, v.D3), new(v.A4, v.B4, v.C4, v.D4));
    public static Quaternion ToUnity(this Assimp.Quaternion v) => new(v.X, v.Y, v.Z, v.W);

    public static BoneWeight AddWeight(this BoneWeight self, int idx, float weight) {
        if (self.weight0 == 0) {
            self.boneIndex0 = idx;
            self.weight0 = weight;
        } else if (self.weight1 == 0) {
            self.boneIndex1 = idx;
            self.weight1 = weight;
        } else if (self.weight2 == 0) {
            self.boneIndex2 = idx;
            self.weight2 = weight;
        } else if (self.weight3 == 0) {
            self.boneIndex3 = idx;
            self.weight3 = weight;
        } else {
            throw new System.InvalidOperationException("Too many weights on one bone!");
        }
        return self;
    }
}

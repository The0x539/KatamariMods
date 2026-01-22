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
}

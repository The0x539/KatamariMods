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
}

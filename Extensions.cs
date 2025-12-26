using UnityEngine;

namespace KatamariDama60;

internal static class Extensions {
    public static GameObject[] GetChildren(this GameObject parent) {
        var ret = new GameObject[parent.transform.childCount];
        for (var i = 0; i < ret.Length; i++) {
            ret[i] = parent.transform.GetChild(i).gameObject;
        }
        return ret;
    }

    /*
    public static IEnumerable<T> GetComponentsInDescendants<T>(this GameObject obj)
    where T : Component {
        var s = new Stack<GameObject>();
        s.Push(obj);
        return GetComponentsInDescendantsCore<T>(s, false);
    }

    private static IEnumerable<T> GetComponentsInDescendantsCore<T>(Stack<GameObject> stack, bool includeSelf)
        where T : Component {
        var includeComponents = includeSelf;

        while (stack.Count > 0) {
            var current = stack.Pop();
            if (includeComponents) {
                if (current.GetComponent<T>() is T component) {
                    yield return component;
                }
            }

            foreach (var child in current.GetChildren()) {
                stack.Push(child);
            }
            includeComponents = true;
        }
    }
    */
}

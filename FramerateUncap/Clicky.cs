using UnityEngine;

namespace FramerateUncap;

public sealed class Clicky : MonoBehaviour {
    public static void Init() {
        var obj = new GameObject("PS2 Inspector");
        obj.AddComponent<Clicky>();
        DontDestroyOnLoad(obj);
    }

    public void Update() {
        KatamariFfi.UpdateUI();

        var cam = Camera.main;
        if (cam == null) return;

        if (!Input.GetMouseButtonDown(0)) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var prop = Inspect(ray);
        if (prop == null) return;

        KatamariFfi.PickThing((ushort)prop.monoControlIndex);
    }

    private static AttachableProp? Inspect(Ray ray) {
        if (!Physics.Raycast(ray, out var hit, 1000f)) return null;
        var fromWorld = hit.collider.gameObject.GetComponentInParent<AttachableProp>();
        if (fromWorld != null) return fromWorld;

        if (GlobalWork.instance?.listProp == null) return null;

        foreach (var prop in GlobalWork.instance.listProp) {
            if (prop == null || !prop.IsAttachedToKatamari) continue;
            foreach (var collider in prop.mMeshColliders) {
                if (collider == null) continue;
                collider.enabled = true;
                var didHit = collider.Raycast(ray, out _, 1000f);
                collider.enabled = false;
                if (didHit) return prop;
            }
        }

        return null;
    }
}

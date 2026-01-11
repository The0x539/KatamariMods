using System;

using UnityEngine;

namespace FramerateUncap;

public sealed class Clicky : MonoBehaviour {
    public void Update() {
        KatamariFfi.UpdateUI();

        var cam = Camera.main;
        if (cam == null) return;

        if (!Input.GetMouseButtonDown(0)) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f)) return;

        var prop = hit.collider.gameObject.GetComponentInParent<AttachableProp>();
        if (prop == null) return;

        KatamariFfi.PickThing((ushort)prop.monoControlIndex);
    }
}

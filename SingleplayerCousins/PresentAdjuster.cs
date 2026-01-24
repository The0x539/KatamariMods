using System.Collections.Generic;

using UnityEngine;

namespace SingleplayerCousins;

public sealed class PresentAdjuster : MonoBehaviour {
    private SkinnedMeshRenderer smr = null!;
    private Mesh mesh = null!;

    public void Awake() {
        this.smr ??= this.GetComponent<SkinnedMeshRenderer>();
        this.mesh ??= this.smr.sharedMesh;
    }

    public void Setup() {
        this.Awake();

        var cousin = this.DetermineCousin();
        var present = this.DeterminePresent();

        if (PresentAdjustments.Get(cousin, present) is not Adjustment adjustment) return;

        var parents = new HashSet<int>();
        foreach (var weight in this.mesh.boneWeights) {
            parents.Add(weight.boneIndex0);
        }

        var bones = this.smr.bones;

        foreach (var idx in parents) {
            var parent = bones[idx];
            if (parent.name.StartsWith("PRE_")) continue;

            var child = new GameObject();
            child.transform.SetParent(parent, worldPositionStays: false);
            child.name = $"PRE_{present}_{parent.name}";

            if (cousin == Cousin.Nickel && present == Present.Camera) {
                child.transform.parent = parent.parent;
            }

            adjustment.ApplyTo(child.transform);

            if (adjustment.overrides?.TryGetValue(parent.name, out var ov) ?? false) {
                ov.ApplyTo(child.transform);
            }

            bones[idx] = child.transform;
        }

        smr.bones = bones;
    }

    private Cousin DetermineCousin() {
        for (var t = this.transform; t != null; t = t.parent) {
            if (t.name.StartsWith("OUJI")) {
                var num = int.Parse(t.name.Substring(4, length: 2));
                return (Cousin)num;
            }
        }
        return 0;
    }

    private Present DeterminePresent() {
        var num = int.Parse(this.name.Substring(4, length: 2));
        return (Present)num;
    }
}

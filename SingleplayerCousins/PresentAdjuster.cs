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

        if (!adjustments.TryGetValue(new(cousin, present), out var adjustment)) return;

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

            if (adjustment.position is Vector3 pos) child.transform.localPosition = pos;
            if (adjustment.rotation is Quaternion rot) child.transform.localRotation = rot;
            if (adjustment.scale is Vector3 scale) child.transform.localScale = scale;

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


    private struct Adjustment {
        public Vector3? position;
        public Vector3? scale;
        public Quaternion? rotation;
    }

    private readonly record struct Combination(Cousin Cousin, Present Present);
    private class Adjustments : Dictionary<Combination, Adjustment> {
        public Adjustment this[Cousin cousin, params Present[] presents] {
            set {
                foreach (var p in presents) {
                    this.Add(new Combination(cousin, p), value);
                }
            }
        }
    }

    private static readonly Adjustments adjustments = new() {
        [Cousin.Odeko, Present.Crown, Present.ChefHat]
            = new() { position = new(0, 0.79f, 0) },

        [Cousin.Odeko, Present.Wig] // this one just sucks basically unavoidably, and many will, due to differing face contours
            = new() { position = new(0, 0.1f, 0.04f), rotation = Quaternion.Euler(55f, 0, 0), scale = new(1.1f, 1.1f, 1f) },

        [Cousin.Havana, Present.Wig]
            = new() { scale = new(1.5f, 1, 1) },

        [Cousin.Havana, Present.Headphones]
            = new() { scale = new(1.75f, 1, 1) },
    };
}

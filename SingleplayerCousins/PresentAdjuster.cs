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


    private struct Adjustment {
        public Vector3? position;
        public Vector3? scale;
        public Quaternion? rotation;

        public Dictionary<string, Adjustment>? overrides;

        public readonly void ApplyTo(Transform t) {
            if (this.position is Vector3 pos) t.localPosition = pos;
            if (this.rotation is Quaternion rot) t.localRotation = rot;
            if (this.scale is Vector3 scale) t.localScale = scale;
        }
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
        [Cousin.Nik, Present.Apron]
            = new() { scale = new(2, 1, 1.75f) },

        [Cousin.Nik, Present.RunningTop]
            = new() {
                scale = new(1.6f, 1, 1.5f),
                overrides = new() {
                    ["JNT_waist"] = new() { scale = new(1.4f, 1, 1.3f) },
                }
            },

        [Cousin.Nik, Present.Mawashi]
            = new() { scale = new(1.25f, 1, 1.125f) },

        [Cousin.Nik, Present.CoolMask]
            = new() { position = new(0, 0.05f, 0) },

        [Cousin.Nik, Present.ChefHat, Present.Crown]
            = new() { position = new(0, 0.07f, 0) },

        [Cousin.Nik, Present.Headphones]
            = new() { position = new(0, 0.025f, 0) },

        [Cousin.Nik, Present.ChampBelt]
            = new() { scale = new(1.4f, 1, 1.25f), position = new(0, 0, 0.01f) },

        [Cousin.Nik, Present.WhiteGuitar]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { position = new(-0.1f, 0, 0), scale = new(3, 1, 1.5f) },
                    ["JNT_spine_01"] = new() { position = new(0.3f, 0.2f, 0), scale = new(1, 1, 2) },
                }
            },

        [Cousin.Nik, Present.AlohaSet]
            = new() {
                scale = new(1.4f, 1, 1.3f),
                position = new(0, 0, 0.01f),
                overrides = new() {
                    ["JNT_spine_01"] = new() { scale = new(1, 1, 1.4f) },
                },
            },

        [Cousin.Nik, Present.SuperheroScarf]
            = new() { scale = new(1.6f, 1, 1.25f) },

        [Cousin.Nik, Present.Wig]
            = new() { scale = new(1.25f, 1.5f, 1), position = new(0, 0.01f, 0.005f) },

        [Cousin.Nik, Present.Camera]
            = new() { scale = new(1.3f, 1.3f, 1.3f) },

        [Cousin.Nik, Present.Ducky]
            = new() { scale = new(1.35f, 1, 1.3f) },

        [Cousin.Nik, Present.WinterScarf]
            = new() { scale = new(1.3f, 1, 1.3f) },

        [Cousin.Nik, Present.Snorkel]
            = new() { scale = new(1, 1.25f, 1.25f), rotation = Quaternion.Euler(-20, 0, 0) },

        [Cousin.Johnson, Present.ChefHat, Present.Crown]
            = new() { position = new(0, 0.05f, 0) },

        [Cousin.Johnson, Present.CoolMask]
            = new() { position = new(0, 0, 0.3f) },

        // Not even going to attempt the wig on Johnson. His face is circular!

        [Cousin.Johnson, Present.Headphones]
            = new() { scale = new(0.6f, 1, 1) },

        [Cousin.Johnson, Present.Snorkel]
            = new() { scale = new(0.8f, 1, 0.5f), position = new(0, 0, 0.35f), rotation = Quaternion.Euler(15, 0, 0) },

        [Cousin.Velvet, Present.Apron]
            = new() { scale = new(1, 1, 1.65f) },

        [Cousin.Velvet, Present.RunningTop]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { scale = new(1.2f, 1, 1.5f) },
                    ["JNT_spine_01"] = new() { scale = new(1, 1, 1.1f) },
                },
            },

        [Cousin.Velvet, Present.Mawashi]
            = new() { position = new(0, -0.08f, 0), scale = new(1.5f, 1, 1.8f), },

        [Cousin.Velvet, Present.ChampBelt]
            = new() { position = new(0, 0.07f, 0), scale = new(0.9f, 1, 1) },

        [Cousin.Velvet, Present.AlohaSet]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { position = new(0, -0.12f, 0), scale = new(1.6f, 1, 2.1f) },
                }
            },

        [Cousin.Velvet, Present.WhiteGuitar]
            = new() {
                overrides = new() {
                    ["JNT_spine_02"] = new() { scale = new(1, 1, 1.2f), rotation = Quaternion.Euler(15, 0, 0) },
                },
            },

        [Cousin.Fujio, Present.CoolMask]
            = new() { position = new(0, -0.05f, 0.05f) },

        [Cousin.Fujio, Present.ChefHat, Present.Crown]
            = new() { position = new(0, 0.25f, 0.01f), scale = new(1.05f, 1, 1.05f) },

        [Cousin.Fujio, Present.Headphones]
            = new() { position = new(0.05f, 0.14f, 0.05f), scale = new(0.5f, 0.5f, 0.5f), rotation = Quaternion.Euler(-55, 30, 0) },

        [Cousin.Fujio, Present.Wig]
            = new() { position = new(0, 0.035f, 0.05f), rotation = Quaternion.Euler(40, 0, 0) },

        [Cousin.Fujio, Present.Snorkel]
            = new() { position = new(0, -0.04f, 0.16f), scale = new(1, 1.14f, 0.5f), rotation = Quaternion.Euler(-5, 0, 0) },

        [Cousin.Havana, Present.Wig]
            = new() { scale = new(1.5f, 1, 1) },

        [Cousin.Havana, Present.Headphones]
            = new() { scale = new(1.75f, 1, 1) },

        [Cousin.Peso, Present.CoolMask]
            = new() { position = new(0, 0.08f, 0), rotation = Quaternion.Euler(15, 0, 0) },

        [Cousin.Peso, Present.Crown]
            = new() { position = new(0, 0.15f, 0.03f), scale = new(0.7f, 0.7f, 0.7f), rotation = Quaternion.Euler(15, 0, 0) },

        [Cousin.Peso, Present.ChefHat]
            = new() { position = new(0, 0.1f, -0.07f), rotation = Quaternion.Euler(-5, 0, 0) },

        [Cousin.Peso, Present.Headphones]
            = new() { position = new(0, 0.04f, 0), scale = new(0.75f, 0, 0) },

        [Cousin.Peso, Present.Wig]
            = new() { position = new(0, 0.1f, 0.05f), rotation = Quaternion.Euler(50, 0, 0) },

        [Cousin.Peso, Present.Snorkel]
            = new() { position = new(0, 0.06f, 0.06f), rotation = Quaternion.Euler(20, 0, 0) },

        [Cousin.Shikao, Present.CoolMask]
            = new() { position = new(0, 0.04f, 0.04f), rotation = Quaternion.Euler(20, 0, 0) },

        [Cousin.Shikao, Present.ChefHat]
            = new() { position = new(0, 0.1f, 0), scale = new(1.9f, 1, 1.9f), rotation = Quaternion.Euler(8, 0, 0) },

        [Cousin.Shikao, Present.Headphones]
            = new() { scale = new(0.55f, 1, 1), position = new(0, 0.05f, 0), rotation = Quaternion.Euler(0, 45, 0) },

        [Cousin.Shikao, Present.Crown]
            = new() { position = new(0, 0.1f, 0) },

        // again, not doing the wig

        [Cousin.Shikao, Present.Snorkel]
            = new() { position = new(0, 0.1f, 0.1f), rotation = Quaternion.Euler(35, 0, 0) },

        [Cousin.Odeko, Present.Crown, Present.ChefHat]
            = new() { position = new(0, 0.79f, 0) },

        [Cousin.Odeko, Present.CoolMask]
            = new() { position = new(0, 0.08f, 0.02f), rotation = Quaternion.Euler(30, 0, 0) },

        [Cousin.Odeko, Present.Headphones]
            = new() { scale = new(0.62f, 1, 1), rotation = Quaternion.Euler(30, 0, 0) },

        [Cousin.Odeko, Present.Wig] // this one just sucks basically unavoidably, and many will, due to differing face contours
            = new() { position = new(0, 0.1f, 0.04f), rotation = Quaternion.Euler(55f, 0, 0), scale = new(1.1f, 1.1f, 1f) },

        [Cousin.Odeko, Present.Snorkel]
            = new() { position = new(0, 0.05f, 0.06f), rotation = Quaternion.Euler(25, 0, 0) },

        [Cousin.Honey, Present.Headphones]
            = new() { scale = new(1.4f, 1, 1) },

        // Marny is just a disaster, and for some reason his animations are broken outside of gameplay.

        [Cousin.Foomin, Present.Headphones]
            = new() { scale = new(1.2f, 1, 1), rotation = Quaternion.Euler(-30, 0, 0) },

        [Cousin.Colombo, Present.Apron, Present.ChampBelt, Present.AlohaSet]
            = new() { scale = new(1.1f, 1, 1.2f) },

        [Cousin.Colombo, Present.RunningTop]
            = new() {
                scale = new(1.2f, 1, 1.3f),
                overrides = new() {
                    ["JNT_spine_01"] = new() { scale = new(1.1f, 1, 1.5f) },
                    ["JNT_spine_02"] = new() { scale = new(1.2f, 1, 1.2f) },
                },
            },

        [Cousin.Opeo, Present.RunningTop]
            = new() { scale = new(1, 1, 1.1f) },

        [Cousin.Nickel, Present.Apron]
            = new() { scale = new(1.2f, 1, 1.7f) },

        [Cousin.Nickel, Present.RunningTop]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { scale = new(1, 1, 1.2f) },
                    ["JNT_spine_01"] = new() { scale = new(1.3f, 1, 1.3f) },
                    ["JNT_spine_02"] = new() { scale = new(1.2f, 1, 1.7f) },
                },
            },

        [Cousin.Nickel, Present.CoolMask]
            = new() { position = new(0, 0.08f, 0), rotation = Quaternion.Euler(30, 0, 0) },

        [Cousin.Nickel, Present.Headphones]
            = new() { scale = new(1.2f, 1, 1) },

        [Cousin.Nickel, Present.ChampBelt]
            = new() { scale = new(1, 1, 1.2f) },

        [Cousin.Nickel, Present.WhiteGuitar]
            = new() {
                overrides = new() {
                    ["JNT_spine_01"] = new() { position = new(0, 0, 0.1f) },
                    ["JNT_spine_02"] = new() { position = new(0, 0, 0.03f), scale = new(1, 1, 1.3f) },
                },
            },

        [Cousin.Nickel, Present.AlohaSet]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { scale = new(1.1f, 1, 1.4f) },
                    ["JNT_spine_02"] = new() { scale = new(1, 1, 1.5f) },
                },
            },

        [Cousin.Nickel, Present.SuperheroScarf]
            = new() { scale = new(1.3f, 1, 1.5f) },

        [Cousin.Nickel, Present.WinterScarf]
            = new() { scale = new(1.3f, 1, 1.5f) },

        [Cousin.Nickel, Present.Wig]
            = new() { position = new(0, 0.008f, 0.1f), rotation = Quaternion.Euler(-30, 0, 0) },

        [Cousin.Nickel, Present.Camera]
            = new() { position = new(0.02f, 0.14f, -0.15f), scale = new(1, 1, 1.5f) },

        [Cousin.Nickel, Present.Snorkel]
            = new() { position = new(0, 0.1f, 0.04f), rotation = Quaternion.Euler(40, 0, 0) },

        [Cousin.Miso, Present.Apron]
            = new() { position = new(0, 0, 0.01f), scale = new(1.1f, 1, 1.2f) },

        [Cousin.Miso, Present.RunningTop]
            = new() {
                scale = new(1, 1, 1.1f),
                overrides = new() {
                    ["JNT_spine_02"] = new() { position = new(1, 1, 1.2f) },
                },
            },

        [Cousin.Miso, Present.CoolMask]
            = new() { position = new(0, 0.15f, 0.15f), rotation = Quaternion.Euler(30, 0, 0) },

        [Cousin.Miso, Present.ChefHat]
            = new() { position = new(-0.2f, 0.1f, -0.1f), rotation = Quaternion.Euler(75, 35, 0) },

        [Cousin.Miso, Present.Headphones]
            = new() { position = new(0, 0.1f, 0), scale = new(0.5f, 0.5f, 0.5f), rotation = Quaternion.Euler(70, 100, 20) },

        [Cousin.Miso, Present.WhiteGuitar]
            = new() {
                overrides = new() {
                    ["JNT_spine_01"] = new() { scale = new(1, 1, 1.2f) },
                },
            },

        [Cousin.Miso, Present.AlohaSet]
            = new() {
                overrides = new() {
                    ["JNT_waist"] = new() { scale = new(1, 1, 1.3f) },
                },
            },

        [Cousin.Miso, Present.Wig]
            = new() { position = new(0, 0.16f, 0.172f), scale = new(1.1f, 1, 1), rotation = Quaternion.Euler(60, 0, 0) },

        [Cousin.Miso, Present.Camera]
            = new() { position = new(0, 0, 0.05f), scale = new(1, 1, 0.7f) },

        [Cousin.Miso, Present.Snorkel]
            = new() { position = new(0, 0.12f, 0.18f), rotation = Quaternion.Euler(35, 0, 0) },
    };
}

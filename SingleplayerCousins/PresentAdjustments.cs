using System.Collections.Generic;

using UnityEngine;

namespace SingleplayerCousins;

static class PresentAdjustments {
    public static Adjustment? Get(Cousin cousin, Present present) {
        if (!dict.TryGetValue(cousin, out var forCousin)) return null;
        if (!forCousin.TryGetValue(present, out var adj)) return null;
        return adj;
    }

    private static Vector3 Vec3(double x, double y, double z) => new((float)x, (float)y, (float)z);
    private static Adjustment Scale(double x = 1, double y = 1, double z = 1) => new() { scale = Vec3(x, y, z) };
    private static Adjustment Translate(double x = 0, double y = 0, double z = 0) => new() { position = Vec3(x, y, z) };
    private static Adjustment Rotate(float x = 0, float y = 0, float z = 0) => new() { rotation = Quaternion.Euler(x, y, z) };

    private const string
        WAIST = "JNT_waist",
        SPINE1 = "JNT_spine_01",
        SPINE2 = "JNT_spine_02";

    private static readonly Dictionary<Cousin, InnerDict> dict = new OuterDict() {
        [Cousin.Nik] = {
            [Present.Apron] = Scale(x: 2, z: 1.75),
            [Present.RunningTop] = [
                Scale(x: 1.6, z: 1.5),
                Scale(x: 1.4, z: 1.3).On("JNT_waist")
            ],
            [Present.Mawashi] = Scale(x: 1.25, z: 1.125),
            [Present.CoolMask] = Translate(y: 0.05),
            [Present.ChefHat, Present.Crown] = Translate(y: 0.07),
            [Present.Headphones] = [
                Translate(y: .052),
                Scale(x: 1.125),
            ],
            [Present.ChampBelt] = [
                Translate(z: .01),
                Scale(x: 1.4, z: 1.25),
            ],
            [Present.WhiteGuitar] = [
                Translate(x: -.1).On(WAIST),
                Scale(x: 3, z: 1.5).On(WAIST),
                Translate(x: .3, y: .2).On(SPINE1),
                Scale(z: 2).On(SPINE1),
            ],
            [Present.AlohaSet] = [
                Translate(z: .01),
                Scale(x: 1.4, z: 1.3),
                Scale(z: 1.4).On(SPINE1),
            ],
            [Present.SuperheroScarf] = Scale(x: 1.6, z: 1.25),
            [Present.Wig] = [
                Translate(y: .01, z: .005),
                Scale(x: 1.25, y: 1.5),
            ],
            [Present.Camera] = [
                Translate(z: .05),
                Scale(x: 1.3, y: 1.3),
            ],
            [Present.Ducky] = Scale(x: 1.35, z: 1.3),
            [Present.WinterScarf] = Scale(x: 1.3, z: 1.3),
            [Present.Snorkel] = [
                Translate(y: .01),
                Scale(y: 1.25, z: 1.25),
                Rotate(x: -20),
            ],
        },
        [Cousin.Johnson] = {
            [Present.ChefHat, Present.Crown] = Translate(y: .05),
            [Present.CoolMask] = Translate(z: .3),
            // Not even going to attempt the wig on Johnson. His face is circular!
            [Present.Headphones] = Scale(x: .6),
            [Present.Snorkel] = [
                Translate(z: .35),
                Scale(x: .8, z: .5),
                Rotate(x: 15),
            ]
        },
        [Cousin.Velvet] = {
            [Present.Apron] = Scale(z: 1.65),
            [Present.RunningTop] = [
                Scale(x: 1.2, z: 1.5).On(WAIST),
                Scale(z: 1.1).On(SPINE1),
            ],
            [Present.Mawashi] = [
                Translate(y: -.08),
                Scale(x: 1.5, z: 1.8),
            ],
            [Present.ChampBelt] = [
                Translate(y: .07),
                Scale(x: .9),
            ],
            [Present.AlohaSet] = [
                Translate(y: -0.12).On(WAIST),
                Scale(x: 1.6, z: 2.1).On(WAIST),
            ],
            [Present.WhiteGuitar] = [
                Scale(z: 1.2).On(SPINE2),
                Rotate(x: 15).On(SPINE2),
            ],
            [Present.Camera] = [
                Translate(y: .05, z: .05),
                Scale(z: .8),
                Rotate(x: -10),
            ],
        },
        [Cousin.Fujio] = {
            [Present.CoolMask] = Translate(y: -.05, z: .05),
            [Present.ChefHat] = [
                Translate(y: .25, z: .01),
                Scale(x: 1.05, z: 1.05),
            ],
            [Present.Crown] = [
                Translate(.09, .33, -.05),
                Rotate(25, 10, 27),
            ],
            [Present.Headphones] = [
                Translate(.05, .14, .05),
                Scale(.5, .5, .5),
                Rotate(x: -55, y: 30),
            ],
            [Present.Wig] = [
                Translate(y: .035, z: .05),
                Rotate(x: 40),
            ],
            [Present.Snorkel] = [
                Translate(y: -.04, z: .16),
                Scale(y: 1.14, z: .5),
                Rotate(x: -5),
            ],
        },
        [Cousin.Havana] = {
            [Present.Wig] = Scale(x: 1.5),
            [Present.Headphones] = Scale(x: 1.75),
        },
        [Cousin.Peso] = {
            [Present.CoolMask] = [
                Translate(y: .08),
                Rotate(x: 15),
            ],
            [Present.Crown] = [
                Translate(y: .15, z: .03),
                Scale(.7, .7, .7),
                Rotate(x: 15),
            ],
            [Present.ChefHat] = [
                Translate(y: .1, z: -.07),
                Rotate(x: -5),
            ],
            [Present.Headphones] = [
                Translate(y: .04),
                Scale(x: .75),
            ],
            [Present.Wig] = [
                Translate(y: .1, z: .05),
                Rotate(x: 50),
            ],
            [Present.Snorkel] = [
                Translate(y: .06, z: .06),
                Rotate(x: 20),
            ],
        },
        [Cousin.Shikao] = {
            [Present.CoolMask] = [
                Translate(y: .04, z: .04),
                Rotate(x: 20),
            ],
            [Present.ChefHat] = [
                Translate(y: .1),
                Scale(x: 1.9, z: 1.9),
                Rotate(x: 8),
            ],
            [Present.Headphones] = [
                Translate(y: .05),
                Scale(x: .55),
                Rotate(y: 45),
            ],
            [Present.Crown] = Translate(y: .1),
            [Present.Snorkel] = [
                Translate(y: .1, z: .1),
                Rotate(x: 35),
            ],
        },
        [Cousin.Odeko] = {
            [Present.Crown, Present.ChefHat] = Translate(y: .79),
            [Present.CoolMask] = [
                Translate(y: .08, z: .02),
                Rotate(x: 30),
            ],
            [Present.Headphones] = [
                Scale(x: .62),
                Rotate(x: 30),
            ],
            [Present.Wig] = [
                Translate(y: .1, z: .04),
                Scale(x: 1.1, y: 1.1),
                Rotate(x: 55),
            ],
            [Present.Snorkel] = [
                Translate(y: .05, z: .06),
                Rotate(x: 25),
            ],
        },
        [Cousin.Honey] = {
            [Present.Headphones] = Scale(x: 1.4),
        },
        [Cousin.Marny] = {
            // Apron is a lost cause.
            // Running Top is a lost cause, perhaps a bit less so than the apron.
            [Present.Mawashi] = Scale(x: 1.8, z: 2),
            [Present.CoolMask] = Translate(z: 0.15),
            [Present.ChefHat, Present.Crown] = Translate(y: 0.09),
            [Present.Headphones] = [
                Translate(y: -0.11),
                Scale(x: 1.2),
                Rotate(x: -15),
            ],
            [Present.ChampBelt] = Scale(x: 2.2, z: 2.5), // ¯\_(ツ)_/¯
            [Present.WhiteGuitar] = Translate(y: 0.2, z: -0.28).On(SPINE2), // The strap is a lost cause.
            [Present.AlohaSet] = [
                Scale(x: 1.8, z: 2.2).On(WAIST),
                Translate(z: 0.65).On(SPINE1),
                Scale(y: 5, z: 0.2).On(SPINE1),
                Translate(y: 0.42, z: -0.05).On(SPINE2),
            ],
            [Present.SuperheroScarf, Present.WinterScarf] = Translate(-0.07, 0.42, -0.05),
            [Present.Wig] = [
                Translate(y: 0.04, z: 0.17),
                Scale(x: 1.07),
                Rotate(x: 30),
            ],
            [Present.Camera] = [
                Translate(z: 0.25),
                Rotate(x: 40),
            ], // ¯\_(ツ)_/¯
            [Present.Ducky] = Scale(2.2, 1.2, 2.2),
            [Present.Snorkel] = Translate(y: -0.02, z: 0.22),
        },
        [Cousin.Foomin] = {
            [Present.Headphones] = [
                Scale(x: 1.2),
                Rotate(x: -30),
            ],
        },
        [Cousin.Colombo] = {
            [Present.Apron, Present.ChampBelt, Present.AlohaSet] = Scale(x: 1.1, z: 1.2),
            [Present.RunningTop] = [
                Scale(x: 1.2, z: 1.3).On(WAIST),
                Scale(x: 1.1, z: 1.5).On(SPINE1),
                Scale(x: 1.2, z: 1.2).On(SPINE2),
            ],
        },
        [Cousin.Opeo] = {
            [Present.RunningTop] = Scale(z: 1.1),
        },
        [Cousin.Nickel] = {
            [Present.Apron] = Scale(x: 1.2, z: 1.7),
            [Present.RunningTop] = [
                Scale(z: 1.2).On(WAIST),
                Scale(x: 1.3, z: 1.3).On(SPINE1),
                Scale(x: 1.2, z: 1.7).On(SPINE2),
            ],
            [Present.CoolMask] = [
                Translate(y: .08),
                Rotate(x: 30),
            ],
            [Present.Headphones] = Scale(x: 1.2),
            [Present.ChampBelt] = Scale(z: 1.2),
            [Present.WhiteGuitar] = [
                Translate(z: .1).On(SPINE1),
                Translate(z: .03).On(SPINE2),
                Scale(z: 1.3).On(SPINE2),
            ],
            [Present.AlohaSet] = [
                Scale(x: 1.1, z: 1.4).On(WAIST),
                Scale(z: 1.5).On(SPINE2),
            ],
            [Present.SuperheroScarf, Present.WinterScarf] = Scale(x: 1.3, z: 1.5),
            [Present.Wig] = [
                Translate(y: .008, z: .1),
                Rotate(x: -30),
            ],
            [Present.Camera] = [
                Translate(.02, .14, -.15),
                Scale(z: 1.5),
            ],
            [Present.Snorkel] = [
                Translate(y: .1, z: .04),
                Rotate(x: 40),
            ],
        },
        [Cousin.Miso] = {
            [Present.Apron] = [
                Translate(z: .01),
                Scale(x: 1.1, z: 1.2),
            ],
            [Present.RunningTop] = [
                Scale(z: 1.1),
                Scale(z: 1.2).On(SPINE2),
            ],
            [Present.CoolMask] = [
                Translate(y: .15, z: .15),
                Rotate(x: 30),
            ],
            [Present.ChefHat] = [
                Translate(-.2, .1, -.1),
                Rotate(x: 75, y: 35),
            ],
            [Present.Headphones] = [
                Translate(y: .1),
                Scale(.5, .5, .5),
                Rotate(70, 100, 20),
            ],
            [Present.WhiteGuitar] = Scale(z: 1.2).On(SPINE1),
            [Present.AlohaSet] = Scale(z: 1.3).On(WAIST),
            [Present.Wig] = [
                Translate(y: .16, z: .172),
                Scale(x: 1.1),
                Rotate(x: 60),
            ],
            [Present.Camera] = [
                Translate(z: .05),
                Scale(z: .7),
            ],
            [Present.Snorkel] = [
                Translate(y: .12, z: .18),
                Rotate(x: 35),
            ],
        },
        [PretenderId.Dega] = {
            [Present.Apron] = [
                Translate(y: -0.2),
                Scale(x: 0.45, z: 0.6),
            ],
            //[Present.RunningTop] = [],
            [Present.Mawashi] = [
                Translate(y: -0.05),
                Scale(x: 0.5, z: 0.5),
            ],
            [Present.CoolMask] = [
                Translate(y: 0.06, z: -0.04),
                Rotate(x: 15),
                Scale(0.5, 0.5, 0.5),
            ],
            [Present.ChefHat] = Scale(x: 0.7, z: 0.7),
            [Present.Headphones] = [
                Translate(y: 0.07),
                Scale(0.27, 0.5, 0.5),
            ],
            [Present.ChampBelt] = [
                Translate(y: -0.08, z: 0.01),
                Scale(x: 0.55, z: 0.55),
            ],
            [Present.WhiteGuitar] = Scale(0.5, 0.5, 0.5),
            [Present.SuperheroScarf, Present.WinterScarf] = [
                Translate(y: 0.05, z: -0.02),
                Scale(-0.5, 0.5, 0.5),
            ],
            [Present.Crown] = [
                Translate(x: 0.01, y: 0.07),
                Rotate(z: -15),
                Scale(0.5, 0.5, 0.5),
            ],
            [Present.Camera] = Scale(0.5, 0.5, 0.5),
            [Present.Ducky] = Scale(0.5, 0.5, 0.5),
            [Present.Snorkel] = [
                Translate(y: 0.04, z: -0.01),
                Rotate(x: 40),
                Scale(0.5, 0.5, 0.5),
            ],
        },
        [PretenderId.Soyo] = {
            //[Present.Apron] = [],
            //[Present.RunningTop] = [],
            //[Present.Mawashi] = [],
            [Present.CoolMask] = [
                Translate(z: 0.035),
                Rotate(x: 20),
                Scale(0.5, 0.5, 0.5),
            ],
            [Present.ChefHat] = Translate(y: -0.04),
            [Present.Headphones] = [
                Translate(y: 0.04),
                Rotate(x: 15),
                Scale(0.41, 0.4, 0.4),
            ],
            [Present.ChampBelt] = [
                Translate(y: 0.32, z: 0.02),
                Scale(0.65, 0.65, 0.65),
            ],
            [Present.WhiteGuitar] = [
            ],
            [Present.AlohaSet] = [
                Translate(y: 0.32).On(WAIST),
                Scale(x: 0.8).On(WAIST),
            ],
            [Present.SuperheroScarf, Present.WinterScarf] = [
                Translate(y: 0.01),
                Scale(0.5, 0.5, 0.5),
            ],
            [Present.Crown] = [
                Translate(y: 0.04),
                Scale(0.5, 0.5, 0.5),
            ],
            [Present.Camera] = [
                Translate(y: -0.05, z: 0.04),
                Scale(0.7, 0.7, 0.5),
            ],
            [Present.Ducky] = [
                Translate(y: 0.32, z: 0.02),
                Scale(0.7, 0.7, 0.7),
            ],
            [Present.Snorkel] = [
                Translate(y: -0.02, z: 0.032),
                Rotate(x: 30),
                Scale(0.6, 0.6, 0.6),
            ],
        },
    };

    private sealed class OuterDict : Dictionary<Cousin, InnerDict> {
        public new InnerDict this[Cousin key] {
            get {
                if (this.TryGetValue(key, out var existing)) {
                    return existing;
                } else {
                    var fresh = new InnerDict();
                    this.Add(key, fresh);
                    return fresh;
                }
            }
        }

        public InnerDict this[PretenderId key] => this[(Cousin)key];
    }

    private sealed class InnerDict : Dictionary<Present, Adjustment> {
        public Adjustment this[params Present[] keys] {
            set {
                foreach (var key in keys) this.Add(key, value);
            }
        }
    }
}

public sealed class Adjustment : System.Collections.IEnumerable {
    public Vector3? position;

    public Vector3? scale;
    public Quaternion? rotation;

    public Dictionary<string, Adjustment>? overrides;

    public void ApplyTo(Transform t) {
        if (this.position is Vector3 pos) t.localPosition = pos;
        if (this.rotation is Quaternion rot) t.localRotation = rot;
        if (this.scale is Vector3 scale) t.localScale = scale;
    }

    public void Add(Adjustment other) {
        this.position ??= other.position;
        this.scale ??= other.scale;
        this.rotation ??= other.rotation;

        if (other.overrides == null) {
            return;
        } else if (this.overrides == null) {
            this.overrides = new(other.overrides);
            return;
        }

        foreach (var key in other.overrides.Keys) {
            if (this.overrides.TryGetValue(key, out var existing)) {
                existing.Add(other.overrides[key]);
            } else {
                this.overrides[key] = other.overrides[key];
            }
        }
    }

    public Adjustment On(string bone) => new() { overrides = new() { [bone] = this } };

    // Only implementing this interface as a hack to make collection initializers usable.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
}

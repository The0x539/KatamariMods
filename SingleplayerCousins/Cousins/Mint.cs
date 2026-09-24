using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Mint : MonoBehaviour {
    private static readonly string[] allStates = [
        "stop1",
        "stop2",
        "walk_slow",
        "walk_f",
        "walk_b",
        "walk_r_side",
        "walk_l_side",
        "dash_start",
        "dash",
        "dash_end",
        "slope",
        "rolldown",
        "rolldown_turn",
        "habit_f",
        "habit_r",
        "habit_l",
        "turn_l",
        "turn_r",
        "brake_f",
        "brake_b",
        "brake_l",
        "brake_r",
        "water",
        "lookup",
        "jump_start",
        "jump",
        "photo",
        "stairs",
        "lookaround",
        "rest",
        "surprise",
        "shock",
        "hang",
        "present",
        "restive",
        "plesure",
        "vex",
        "washtub",
        "pain",
        "bye",
        "habit_b",
        "select_walk",
        "select_fly",
        "rest_start",
        "attack",
        "shock_stay",
        "walk_f_fast",
        "walk_b_fast",
        "walk_r_side_fast",
        "walk_l_side_fast",
        "turn_l_fast",
        "turn_r_fast",
    ];

    private static readonly Dictionary<int, string> byHash = allStates.ToDictionary(Animator.StringToHash);

    private class Face {
        public float A, E, I, O, U;
        public float Blink;
        public float Angry, Surprise, Blushed, Worried;
        public float CheekPuff;
    };

    private static readonly Face defaultFace = new() { O = 0.75f };
    private static readonly Face highEffortFace = new() { Angry = 1, CheekPuff = 1 };
    private static readonly Face mediumEffortFace = new() { E = 0.5f, U = 0.25f, Worried = 1 };

    private static readonly Dictionary<string, Face> faces = new() {
        ["stop1"] = defaultFace,
        ["lookaround"] = defaultFace,
        ["habit_f"] = defaultFace,
        ["habit_b"] = defaultFace,
        ["habit_l"] = defaultFace,
        ["habit_r"] = defaultFace,
        ["walk_slow"] = defaultFace,
        ["jump"] = defaultFace,

        ["water"] = new() { O = 0.25f, U = 0.5f, Surprise = 1 },
        ["lookup"] = new() { O = 0.25f, U = 0.5f, Surprise = 1 },

        ["pain"] = new() { Blushed = 1 },
        ["stairs"] = highEffortFace,
        ["brake_f"] = highEffortFace,
        ["brake_b"] = highEffortFace,
        ["brake_l"] = highEffortFace,
        ["brake_r"] = highEffortFace,
        ["walk_l_side"] = new() { Worried = 1 },
        ["walk_r_side"] = new() { Worried = 1 },
        ["walk_f"] = mediumEffortFace,
        ["walk_b"] = mediumEffortFace,
        ["turn_l"] = mediumEffortFace,
        ["turn_r"] = mediumEffortFace,
        ["turn_l_fast"] = mediumEffortFace,
        ["turn_r_fast"] = mediumEffortFace,
    };

    private SkinnedMeshRenderer smr = new();
    private Animator anim = new();
    private GameObject[] eyes = [];

    private int _currentAnimHash = -1;
    public int CurrentAnimHash {
        get => this._currentAnimHash;
        set {
            if (value == this._currentAnimHash) return;
            this._currentAnimHash = value;

            if (!byHash.TryGetValue(value, out var name)) {
                Plugin.logger.LogWarning($"No animation name found for hash {value}");
                return;
            }

            if (!faces.TryGetValue(name, out var face)) {
                Plugin.logger.LogWarning($"No face found for animation {name}");
                this.Apply(defaultFace);
                return;
            }

            this.Apply(face);
        }
    }

    private int GetIndex(string name) {
        return this.smr.sharedMesh.GetBlendShapeIndex(name);
    }

    private void SetWeight(string name, float weight) {
        this.smr.SetBlendShapeWeight(this.GetIndex(name), weight);
    }

    private void Apply(Face face) {
        this.SetWeight("vrc.v_a", face.A);
        this.SetWeight("vrc.v_e", face.E);
        this.SetWeight("vrc.v_i", face.I);
        this.SetWeight("vrc.v_o", face.O);
        this.SetWeight("vrc.v_u", face.U);
        //this.SetWeight("vrc.blink_both", face.Blink); // TODO: Reconcile with the blink animation, perhaps by multiplying: 1-((1-a)*(1-b))?
        this.SetWeight("Angry", face.Angry);
        this.SetWeight("Surprise", face.Surprise);
        this.SetWeight("Blushed", face.Blushed);
        this.SetWeight("Worried", face.Worried);
        this.SetWeight("CheekPuff", face.CheekPuff);
    }

    public void Awake() {
        this.smr = this.transform.Find("body_root/Mint64.baked").GetComponent<SkinnedMeshRenderer>();
        this.anim = this.GetComponent<Animator>();
        this.eyes = [.. from i in Enumerable.Range(1, 5) select this.transform.Find($"face_root/eye/eye_0{i}").gameObject];
        this.SetWeight("NoMorePain", 1);
    }

    public void Update() {
        this.CurrentAnimHash = this.anim.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (this.eyes[1].activeSelf) {
            this.SetWeight("vrc.blink_both", 0.5f);
        } else if (this.eyes[2].activeSelf) {
            this.SetWeight("vrc.blink_both", 1f);
        } else {
            this.SetWeight("vrc.blink_both", 0f);
        }
    }
}

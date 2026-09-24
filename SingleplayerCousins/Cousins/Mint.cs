using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Mint : MonoBehaviour {
    private Transform root = new();
    private Animator animator = new();
    private SkinnedMeshRenderer smr = new();

    private int GetIndex(string name) {
        return this.smr.sharedMesh.GetBlendShapeIndex(name);
    }

    private void SetWeight(string name, float weight) {
        this.smr.SetBlendShapeWeight(this.GetIndex(name), weight);
    }

    public void Awake() {
        this.root = this.transform.Find("JNT_root");
        this.animator = this.GetComponent<Animator>();
        this.smr = this.transform.Find("body_root/Mint64.baked").GetComponent<SkinnedMeshRenderer>();

        this.SetWeight("NoMorePain", 1);
    }

    public void Update() {
    }
}

using System.Linq;

using UnityEngine;

namespace SingleplayerCousins;

// Stop over-exaggerated animations by preventing the problematic ones from changing bone position whatsoever.
// Quite a hack, but it'll do if I can't figure out a better way.
public sealed class PreventArmatureExplosion : MonoBehaviour {
    private Animator animator = new();

    private Transform[] bones = [];
    private Vector3[] boneOrigins = [];

    public void Awake() {
        this.bones = this.transform.Find("JNT_root/JNT_waist").GetComponentsInChildren<Transform>();
        this.boneOrigins = this.bones.Select(b => b.localPosition).ToArray();
        this.animator = this.GetComponent<Animator>();
    }

    public void LateUpdate() {
        if (!this.RestrictStretching()) return;

        for (var i = 0; i < this.bones.Length; i++) {
            this.bones[i].localPosition = this.boneOrigins[i];
        }
    }

    // Names of ingame simulation-controlled animations found in CharacterAnimationController.SetClone
    private static readonly int
        peterPanHash = Animator.StringToHash("peter_pan"),
        shockHash = Animator.StringToHash("shock"),
        shockStayHash = Animator.StringToHash("shock_stay");

    private bool RestrictStretching() {
        var currentAnim = this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        return currentAnim == peterPanHash || currentAnim == shockHash || currentAnim == shockStayHash;
    }
}

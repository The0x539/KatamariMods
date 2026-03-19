using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class HandFix : MonoBehaviour {
    private Transform root = new();
    private Transform[] elbows = [], hands = [];
    private Animator animator = new();

    public Vector3 elbowAngle = new(-90, 0, 0);
    public Vector3 handAngle = new(0, 0, 0);
    public Vector3 cameraCounterAngle = new(0, 0, 90);
    public Vector3 cameraPosition = Vector3.zero;
    public float cameraScale = 1f;

    private static readonly int
        surpriseHash = Animator.StringToHash("surprise"),
        selectHash = Animator.StringToHash("select"),
        photoUpHash = Animator.StringToHash("photo_up");

    public void Awake() {
        this.root = this.transform.Find("JNT_root");
        this.animator = this.GetComponent<Animator>();

        this.elbows = [
            this.root.Find("JNT_waist/JNT_spine_01/JNT_spine_02/JNT_shoulder_L/JNT_arm_L/JNT_elbow_L"),
            this.root.Find("JNT_waist/JNT_spine_01/JNT_spine_02/JNT_shoulder_R/JNT_arm_R/JNT_elbow_R"),
        ];
        this.hands = this.elbows.Select(e => e.GetChild(0)).ToArray();
    }

    public void LateUpdate() {
        var anim = this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (anim == photoUpHash || anim == surpriseHash || anim == selectHash) {
            this.elbows[0].Rotate(this.elbowAngle);
            this.elbows[1].Rotate(this.elbowAngle);
            this.hands[0].Rotate(this.handAngle);
            this.hands[1].Rotate(-this.handAngle);
            if (this.hands[1].Find("pre_13(Clone)/pre_root") is Transform camera) {
                camera.localPosition = this.cameraPosition;
                camera.localRotation = Quaternion.Euler(this.cameraCounterAngle);
                camera.localScale = Vector3.one * this.cameraScale;
            }
        }
    }
}

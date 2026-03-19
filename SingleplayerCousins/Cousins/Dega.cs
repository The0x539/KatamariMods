using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Dega : MonoBehaviour {
    private float theta = 0;
    private Transform root = new();

    private Transform[] tail = [];
    private Transform[] arms = [];
    private Quaternion[] tailRestPose = [];

    private Animator animator = new();

    private static readonly int
        peterPanHash = Animator.StringToHash("peter_pan"),
        stairsHash = Animator.StringToHash("stairs"),
        surpriseHash = Animator.StringToHash("surprise"),
        selectHash = Animator.StringToHash("select"),
        photoUpHash = Animator.StringToHash("photo_up");

    public void Awake() {
        this.root = this.transform.Find("JNT_root");
        this.animator = this.GetComponent<Animator>();

        var tail = new List<Transform>();
        var joint = this.root.Find("JNT_waist/JNT_tail");
        while (true) {
            tail.Add(joint);
            if (joint.childCount == 0) {
                break;
            } else {
                joint = joint.GetChild(0);
            }
        }
        this.tail = tail.ToArray();
        this.tailRestPose = this.tail.Select(t => t.localRotation).ToArray();

        this.arms = [
            this.root.Find("JNT_waist/JNT_spine_01/JNT_spine_02/JNT_shoulder_L/JNT_arm_L/JNT_elbow_L"),
            this.root.Find("JNT_waist/JNT_spine_01/JNT_spine_02/JNT_shoulder_R/JNT_arm_R/JNT_elbow_R"),
        ];
    }

    public void Update() {
        var dt = Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime;
        this.theta += dt;

        var anim = this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (anim == peterPanHash) {
            this.tail[0].localRotation = this.tailRestPose[0] * Quaternion.Euler(10, 0, -18) * Quaternion.Euler(0, this.theta * 1200, 0);

            var hover_dy = 0.1f * Mathf.Sin(6.13f * this.theta) * dt;
            this.root.localPosition += new Vector3(0, hover_dy, 0);
        } else if (anim == stairsHash) {
            this.tail[0].localRotation = Quaternion.Euler(-135, 0, 5 * Mathf.Sin(this.theta * 69));
            for (var i = 1; i < this.tail.Length; i++) {
                this.tail[i].localRotation = Quaternion.identity;
            }
        } else {
            float tailBiasXY, tailBiasZ, tailAmplitude;
            // Ugh, the axes are so messed up. This is kind of my own fault, but still. Whatever, this works well enough.
            // No idea why the HUD needs a different bias in the first place. Especially in multiplayer, when the portrait is facing the camera almost dead on.
            if (this.gameObject.scene.name is "UI_HUD" or "UI_HUD_vs") {
                tailBiasXY = 0;
                tailBiasZ = 21.7f;
                tailAmplitude = 56.7f;
            } else {
                tailBiasXY = 20f;
                tailBiasZ = -22.3f;
                tailAmplitude = 23.21f;
            }

            var baseTwist = tailAmplitude * Mathf.Sin(this.theta * 1.56f) + tailBiasZ;
            this.tail[0].localRotation = this.tailRestPose[0] * Quaternion.Euler(tailBiasXY, tailBiasXY, baseTwist);

            for (var i = 1; i < this.tail.Length; i++) {
                var twist = Mathf.Sin(this.theta * 1.27f - (i * 0.87f)) * 19f * Mathf.Sqrt(i);
                this.tail[i].localRotation = this.tailRestPose[i] * Quaternion.Euler(0, 0, twist);
            }
        }
    }

    public void LateUpdate() {
        var anim = this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (anim == photoUpHash || anim == surpriseHash || anim == selectHash) {
            this.arms[0].Rotate(-90, 0, 0);
            this.arms[1].Rotate(-90, 0, 0);
            if (this.arms[1].Find("JNT_hand_R/pre_13(Clone)/pre_root") is Transform camera) {
                camera.localRotation = Quaternion.Euler(0, 0, 90);
                camera.localPosition = new(0.13f, -0.02f, 0);
                camera.localScale = Vector3.one * 0.725f;
            }
        }
    }
}

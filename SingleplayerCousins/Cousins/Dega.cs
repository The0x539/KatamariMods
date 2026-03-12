using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Dega : MonoBehaviour {
    private float theta = 0;
    private Transform root = new();

    private Transform[] tail = [];
    private Quaternion[] tailRestPose = [];

    private Animator animator = new();

    private static readonly int peterPanHash = Animator.StringToHash("peter_pan");

    public void Awake() {
        this.root = this.transform.Find("JNT_root");
        this.animator = this.GetComponent<Animator>();
        if (this.gameObject.GetComponent<PreventArmatureExplosion>() == null) {
            this.gameObject.AddComponent<PreventArmatureExplosion>();
        }

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
    }

    public void Update() {
        var dt = Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime;
        this.theta += dt;

        if (this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash == peterPanHash) {
            this.tail[0].localRotation = this.tailRestPose[0] * Quaternion.Euler(0, -20, 0) * Quaternion.Euler(0, this.theta * 1200f, 0);

            var hover_dy = 0.1f * Mathf.Sin(6.13f * this.theta) * dt;
            this.root.localPosition += new Vector3(0, hover_dy, 0);
        } else {
            // TODO: Ever since preparing the bindposes or whatever, this is all messed up.
            float y = 1.2f * Mathf.Sin(this.theta * 0.431f + 1.365f), // Frequency and phase chosen arbitrarily to stop the axes from harmonizing
                  z = 50 * Mathf.Sin(this.theta) - 35,                // The tail on the model naturally faces ~30-35 degrees to the left.
                  x = 5 * Mathf.Sin(this.theta * 0.789f + 2.718f);
            this.tail[0].localRotation = this.tailRestPose[0] * Quaternion.Euler(x, y, z);
        }
    }
}

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Dega : MonoBehaviour {
    private float theta = 0;
    private Transform
        tail = new(),
        root = new();

    private Animator animator = new();

    private static readonly int peterPanHash = Animator.StringToHash("peter_pan");

    public void Awake() {
        this.root = this.transform.Find("JNT_root");
        this.tail = this.root.Find("JNT_waist/JNT_tail");
        this.animator = this.GetComponent<Animator>();
        if (this.gameObject.GetComponent<PreventArmatureExplosion>() == null) {
            this.gameObject.AddComponent<PreventArmatureExplosion>();
        }
    }

    // TODO: Animate multiple joints (probably 3-4?) along the tail's length rather than treating it as one solid object

    public void Update() {
        var dt = Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime;
        this.theta += dt;

        if (this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash == peterPanHash) {
            this.tail.localRotation = Quaternion.Euler(0, -20, 0) * Quaternion.Euler(0, 0, this.theta * 1200f);

            var hover_dy = 0.1f * Mathf.Sin(6.13f * this.theta) * dt;
            this.root.localPosition += new Vector3(0, hover_dy, 0);
        } else {
            float x = 4 * Mathf.Sin(this.theta * 0.431f + 1.365f), // Frequency and phase chosen arbitrarily to stop the axes from harmonizing
                  y = 50 * Mathf.Sin(this.theta) - 35,             // The tail on the model naturally faces ~30-35 degrees to the left.
                  z = 5 * Mathf.Sin(this.theta * 0.789f + 2.718f);
            this.tail.localRotation = Quaternion.Euler(x, y, z);
        }
    }
}

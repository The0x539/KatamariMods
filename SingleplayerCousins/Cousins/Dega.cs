using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Dega : MonoBehaviour {
    private float theta = 0;
    private Transform tail = new();

    public void Awake() {
        this.tail = this.transform.Find("JNT_root/JNT_waist/JNT_tail");
        if (this.gameObject.GetComponent<PreventArmatureExplosion>() == null) {
            this.gameObject.AddComponent<PreventArmatureExplosion>();
        }
    }

    // TODO: Animate multiple joints (probably 3-4?) along the tail's length rather than treating it as one solid object

    public void Update() {
        this.theta += Time.deltaTime;
        float x = 4 * Mathf.Sin(this.theta * 0.431f + 1.365f), // Frequency and phase chosen arbitrarily to stop the axes from harmonizing
              y = 50 * Mathf.Sin(this.theta) - 35,             // The tail on the model naturally faces ~30-35 degrees to the left.
              z = 5 * Mathf.Sin(this.theta * 0.789f + 2.718f);
        this.tail.localRotation = Quaternion.Euler(x, y, z);
    }
}

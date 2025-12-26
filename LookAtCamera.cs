using UnityEngine;

namespace KatamariDama60;

public sealed class LookAtCamera : MonoBehaviour {
    public Camera? camera = null;

    public void Update() {
        if (this.camera is Camera c) {
            this.transform.LookAt(c.transform);
        }
    }
}

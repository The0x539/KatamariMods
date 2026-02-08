using UnityEngine;

namespace SingleplayerCousins;

public sealed class Marny : MonoBehaviour {
    private Transform? spine, neck, head;

    public void Start() {
        this.spine = this.transform.Find("JNT_root/JNT_waist/JNT_spine_01/JNT_spine_02");
        this.neck = this.spine.Find("JNT_neck");
        this.head = this.neck.Find("JNT_head");
    }

    public void LateUpdate() {
        this.spine?.localRotation = Quaternion.identity;
        this.neck?.localRotation = Quaternion.identity;
        this.head?.localRotation = Quaternion.identity;
    }
}

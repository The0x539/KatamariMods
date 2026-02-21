using System.Collections;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins.Cousins;

public sealed class Dipp : MonoBehaviour {
    private Material[] materials = [];
    private static readonly Vector2[] positions = [
        new(0, 0),
        new(0.5f, 0),
        new(0, -0.25f),
        new(0.5f, -0.25f),
        new(0, -0.5f),
    ];

    public void Start() {
        this.materials = new[] { "body_m", "head_m", "hand_m" }
            .Select(name => this.transform.Find($"body_root/{name}"))
            .Select(part => part.GetComponent<SkinnedMeshRenderer>())
            .Select(r => r.material)
            .ToArray();

        this.StartCoroutine(this.Animate());
    }

    private IEnumerator Animate() {
        var i = 0;
        while (true) {
            foreach (var m in this.materials) {
                m.mainTextureOffset = positions[i];
            }
            i = (i + 1) % positions.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
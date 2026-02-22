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
    }

    private float timer = 0;
    private int posIndex = 0;

    public void Update() {
        this.timer += Time.deltaTime;
        if (this.timer < 0.5) return;
        this.timer -= 0.5f;

        foreach (var m in this.materials) {
            m.mainTextureOffset = positions[this.posIndex];
        }
        this.posIndex = (this.posIndex + 1) % positions.Length;
    }
}
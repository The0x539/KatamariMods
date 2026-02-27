using MyGame;
using MyGame.InputStatus;

using UnityEngine;

namespace DevUtils;

public class KonamiCode : MonoBehaviour {
    public static readonly KeyMap[] sequence = [
        KeyMap.Up, KeyMap.Up, KeyMap.Down, KeyMap.Down,
        KeyMap.Left, KeyMap.Right, KeyMap.Left, KeyMap.Right,
        KeyMap.B, KeyMap.A, KeyMap.Back,
    ];

    private int sequenceIndex = 0;

    public bool IsPrimed => this.sequenceIndex >= sequence.Length;
    public void Reset() => this.sequenceIndex = 0;

    public void Start() => this.Reset();

    public void LateUpdate() {
        var pad = InputController.Instance.Pad(0);
        if (pad == null) return;

        if (this.IsPrimed) {
            if (pad.IsDown(KeyMap.Start)) { } else if (pad.IsAnyDown()) {
                this.Reset();
            }
        } else if (pad.IsDown(sequence[this.sequenceIndex])) {
            this.sequenceIndex++;
        } else if (pad.IsAnyDown()) {
            this.Reset();
        }
    }
}

using GamepadSupport.SteamInput;

using MyGame;
using MyGame.InputStatus;

using System;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

public sealed class InputPadSDL3 : InputPadBase {
    private SDL.Gamepad? inner;
    public SDL.Gamepad Inner {
        get {
            if (this.inner == null) throw new InvalidOperationException();
            return this.inner;
        }
    }

    private readonly float[] axes = new float[(int)SDL.GamepadAxis.COUNT];
    private uint buttons;

    static InputPadSDL3() {
        ISteamInput.Instance.Init();
    }

    public void Start() {
        this.Enabled = true;
    }

    public void Connect(SDL.JoystickID id) {
        this.inner = new SDL.Gamepad(id);
        this.inner.PlayerIndex = this.ID;
        var steam = this.inner.Steam;

        ISteamInput.Instance.RunFrame();

        Console.WriteLine($"Input type: {steam.InputType}");

        XboxOrigin[] buttons = { XboxOrigin.A, XboxOrigin.B, XboxOrigin.X, XboxOrigin.Y };
        foreach (var button in buttons) {
            var actionOrigin = steam.GetActionOriginFromXboxOrigin(button);
            string path = ISteamInput.Instance.GetGlyphPNGForActionOrigin(actionOrigin, GlyphSize.Medium, 0);
        }
    }

    // Corresponds to MyGame.InputStatus.KeyMap
    private static readonly SDL.GamepadButton[] buttonSequence = [
        SDL.GamepadButton.South,
        SDL.GamepadButton.East,
        SDL.GamepadButton.West,
        SDL.GamepadButton.North,
        SDL.GamepadButton.LeftShoulder,
        SDL.GamepadButton.RightShoulder,
        SDL.GamepadButton.Back,
        SDL.GamepadButton.Start,
        SDL.GamepadButton.LeftStick,
        SDL.GamepadButton.RightStick,
        SDL.GamepadButton.COUNT, // left trigger
        SDL.GamepadButton.COUNT, // right trigger
        SDL.GamepadButton.COUNT, // home
        SDL.GamepadButton.COUNT, // left joycon, left shoulder
        SDL.GamepadButton.COUNT, // left joycon, right shoulder
        SDL.GamepadButton.COUNT, // right joycon, left shoulder
        SDL.GamepadButton.COUNT, // right joycon, right shoulder
        SDL.GamepadButton.DpadLeft,
        SDL.GamepadButton.DpadRight,
        SDL.GamepadButton.DpadUp,
        SDL.GamepadButton.DpadDown,
        SDL.GamepadButton.COUNT, // left stick, left
        SDL.GamepadButton.COUNT, // left stick, right
        SDL.GamepadButton.COUNT, // left stick, up
        SDL.GamepadButton.COUNT, // left stick, down
        SDL.GamepadButton.COUNT, // right stick, left
        SDL.GamepadButton.COUNT, // right stick, right
        SDL.GamepadButton.COUNT, // right stick, up
        SDL.GamepadButton.COUNT, // right stick, down
        // Things past this point don't seem to be necessary.
    ];

    // TODO: properly bring back keyboard input?
    public override bool IsKeybord => this.inner == null;
    public override bool IsConnectPad => this.inner != null;
    public override int ConnectCount => this.IsConnectPad ? 1 : 0;

    public override PadType PadType => this.inner?.GamepadType switch {
        SDL.GamepadType.PS3 or
        SDL.GamepadType.PS4 or
        SDL.GamepadType.PS5 => PadType.PS4,

        SDL.GamepadType.SwitchPro => PadType.SwitchProcon,
        SDL.GamepadType.SwitchJoyConLeft => PadType.SwitchJoyLeft,
        SDL.GamepadType.SwitchJoyConRight => PadType.SwitchJoyRight,
        SDL.GamepadType.SwitchJoyConPair => PadType.SwitchJoyHandheld,

        SDL.GamepadType.Xbox360 or
        SDL.GamepadType.XboxOne => PadType.Xbox,

        _ => PadType.Steam,
    };

    public override PadMode PadMode => this.inner?.GamepadType switch {
        SDL.GamepadType.SwitchJoyConPair => PadMode.Dual,
        SDL.GamepadType.SwitchJoyConLeft or
        SDL.GamepadType.SwitchJoyConRight => PadMode.Half,
        _ => PadMode.Single,
    };

    public override IconType IconType => this.inner?.GamepadType switch {
        SDL.GamepadType.PS3 or
        SDL.GamepadType.PS4 or
        SDL.GamepadType.PS5 => IconType.PS4,

        SDL.GamepadType.SwitchPro or
        SDL.GamepadType.SwitchJoyConLeft or
        SDL.GamepadType.SwitchJoyConRight or
        SDL.GamepadType.SwitchJoyConPair => IconType.Switch,

        _ => IconType.PC,
    };

    public override void Vibration(float time) {
        var low = (ushort)(this.motorStrengthL << 8);  // "L(ong)" wavelength = "low" frequency ("left" motor, practically)
        var high = (ushort)(this.motorStrengthS << 8); // "S(hort)" wavelength = "high" frequency ("right" motor, practically)
        this.inner?.Rumble(low, high, (uint)(time * 1000));
    }

    private float GetAxis(SDL.GamepadAxis axis) => this.axes[(int)axis];

    public void Update() {
        if (this.inner != null && !this.inner.IsConnected) {
            this.inner = null;
        }
    }

    private void ReadAxes() {
        if (this.inner is null) {
            for (var axis = 0; axis < (int)SDL.GamepadAxis.COUNT; axis++) this.axes[axis] = 0f;
            return;
        }

        for (var axis = (SDL.GamepadAxis)0; axis < SDL.GamepadAxis.COUNT; axis++) {
            this.axes[(int)axis] = this.inner.GetAxis(axis) switch {
                0 => 0f,
                32767 => 1f,
                -32768 => -1f,
                > -5000 and < 5000 => 0, // deadzone
                short n => n / 32767f,
            };
        }
    }

    private void ReadButtons() {
        if (this.inner is null) {
            this.buttons = 0;
            return;
        }

        var buttons = 0u;
        for (var i = 0; i < buttonSequence.Length; i++) {
            var b = buttonSequence[i];
            var pressed = this.inner.GetButton(b);

            pressed |= i switch {
                10 => this.GetAxis(SDL.GamepadAxis.LeftTrigger) > 0.8,
                11 => this.GetAxis(SDL.GamepadAxis.RightTrigger) > 0.8,
                17 or 21 => this.GetAxis(SDL.GamepadAxis.LeftX) < -0.4,
                18 or 22 => this.GetAxis(SDL.GamepadAxis.LeftX) > 0.4,
                19 or 23 => this.GetAxis(SDL.GamepadAxis.LeftY) < -0.4,
                20 or 24 => this.GetAxis(SDL.GamepadAxis.LeftY) > 0.4,
                25 => this.GetAxis(SDL.GamepadAxis.RightX) < -0.4,
                26 => this.GetAxis(SDL.GamepadAxis.RightX) > 0.4,
                27 => this.GetAxis(SDL.GamepadAxis.RightY) < -0.4,
                28 => this.GetAxis(SDL.GamepadAxis.RightY) > 0.4,
                _ => false,
            };

            if (pressed) {
                buttons |= 1u << i;
            }
        }
        this.buttons = buttons;
    }

    public override void Tick() {
        if (!this.Enabled) return;

        this.ReadAxes();
        this.ReadButtons();

        var lx = this.GetAxis(SDL.GamepadAxis.LeftX);
        var ly = this.GetAxis(SDL.GamepadAxis.LeftY);
        var rx = this.GetAxis(SDL.GamepadAxis.RightX);
        var ry = this.GetAxis(SDL.GamepadAxis.RightY);
        this.PushStickLeft(lx, -ly);
        this.PushStickRight(rx, -ry);
        this.PushL2Trigger(this.GetAxis(SDL.GamepadAxis.LeftTrigger));
        this.PushR2Trigger(this.GetAxis(SDL.GamepadAxis.RightTrigger));
        this.PushButton(this.buttons);
        var select = (this.buttons & 0b01) != 0;
        var cancel = (this.buttons & 0b10) != 0;
        this.PushSelectCancel(select, cancel);
    }
}
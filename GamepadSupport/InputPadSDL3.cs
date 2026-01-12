using MyGame;
using MyGame.InputStatus;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

public sealed class InputPadSDL3 : InputPadBase {
    private SDL.Gamepad inner;

    private readonly float[] axes = new float[(int)SDL.GamepadAxis.Count];
    private uint buttons;

    public void Start() {
        // TODO: Properly handle hotplugging and figure out the deal with multiplayer
        // SDL_GetGamepadFromPlayerIndex seems relevant
        this.inner = new SDL.Gamepad((SDL.JoystickID)this.ID + 1);
        this.Enabled = true;
    }

    // Corresponds to MyGame.InputStatus.KeyMap
    private static readonly SDL.GamepadButton[] buttonSequence = [
        SDL.GamepadButton.South,
        SDL.GamepadButton.East,
        SDL.GamepadButton.West,
        SDL.GamepadButton.North,
        SDL.GamepadButton.LeftShoulder,
        SDL.GamepadButton.RightShoulder,
        SDL.GamepadButton.Guide,
        SDL.GamepadButton.Start,
        SDL.GamepadButton.LeftStick,
        SDL.GamepadButton.RightStick,
        SDL.GamepadButton.Count, // left trigger
        SDL.GamepadButton.Count, // right trigger
        SDL.GamepadButton.Count, // home
        SDL.GamepadButton.Count, // left joycon, left shoulder
        SDL.GamepadButton.Count, // left joycon, right shoulder
        SDL.GamepadButton.Count, // right joycon, left shoulder
        SDL.GamepadButton.Count, // right joycon, right shoulder
        SDL.GamepadButton.DpadLeft,
        SDL.GamepadButton.DpadRight,
        SDL.GamepadButton.DpadUp,
        SDL.GamepadButton.DpadDown,
        SDL.GamepadButton.Count, // left stick, left
        SDL.GamepadButton.Count, // left stick, right
        SDL.GamepadButton.Count, // left stick, up
        SDL.GamepadButton.Count, // left stick, down
        SDL.GamepadButton.Count, // right stick, left
        SDL.GamepadButton.Count, // right stick, right
        SDL.GamepadButton.Count, // right stick, up
        SDL.GamepadButton.Count, // right stick, down
        // After this is the eight directions (four per stick). TBD if they, or anything past this point, is necessary.
    ];

    public override bool IsKeybord => false;
    public override bool IsConnectPad => inner.IsConnected;
    public override int ConnectCount => 1;

    public override PadType PadType => this.inner.GamepadType switch {
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

    public override PadMode PadMode => this.inner.GamepadType switch {
        SDL.GamepadType.SwitchJoyConPair => PadMode.Dual,
        SDL.GamepadType.SwitchJoyConLeft or
        SDL.GamepadType.SwitchJoyConRight => PadMode.Half,
        _ => PadMode.Single,
    };

    public override IconType IconType => this.inner.GamepadType switch {
        SDL.GamepadType.PS3 or
        SDL.GamepadType.PS4 or
        SDL.GamepadType.PS5 => IconType.PS4,

        SDL.GamepadType.SwitchPro or
        SDL.GamepadType.SwitchJoyConLeft or
        SDL.GamepadType.SwitchJoyConRight or
        SDL.GamepadType.SwitchJoyConPair => IconType.Switch,

        _ => IconType.PC,
    };

    // WIP: I can't even get the game to allow enabling vibration yet
    public override void Vibration(float time) {
        this.inner.Rumble(0x7FFF, 0, (uint)(time * 1000));
    }

    private float GetAxis(SDL.GamepadAxis axis) => this.axes[(int)axis];

    private void ReadAxes() {
        for (var axis = (SDL.GamepadAxis)0; axis < SDL.GamepadAxis.Count; axis++) {
            this.axes[(int)axis] = this.inner.GetAxis(axis) switch {
                0 => 0f,
                32767 => 1f,
                -32768 => -1f,
                > -768 and < 768 => 0, // deadzone
                short n => n / 32767f,
            };
        }
    }

    private void ReadButtons() {
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

        SDL.Gamepad.Update();
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
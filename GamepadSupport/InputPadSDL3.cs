using BepInEx.Configuration;

using GamepadSupport.SteamInput;

using MyGame;
using MyGame.InputStatus;

using System;

using UnityEngine;

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

    private static readonly SDL.SensorType[] motionSensors = [
        SDL.SensorType.AccelLeft, SDL.SensorType.AccelRight,
        SDL.SensorType.GyroLeft, SDL.SensorType.GyroRight,
    ];

    static InputPadSDL3() {
        ISteamInput.Instance.Init();
    }

    public void Start() {
        this.Enabled = true;
    }

    public void Connect(SDL.JoystickID id) {
        var sdl = new SDL.Gamepad(id);
        if (sdl.Name is null or "") {
            Plugin.Log.LogError($"Joystick {id} has blank name. Ignoring on the assumption that it's invalid.");
            return;
        }

        this.inner = sdl;
        this.inner.PlayerIndex = this.ID;

        /*
        if (this.inner.GamepadType == SDL.GamepadType.SwitchJoyConPair && this.HasAllMotionSensors()) {
            this.SetMotionSensorsEnabled(true);
        }
        */

        Plugin.Log.LogInfo($"Connected joystick {id} for player {this.ID}: {this.Inner.Name}");
        ISteamInput.Instance.RunFrame();
    }

    private bool HasAllMotionSensors() {
        foreach (var sensor in motionSensors) {
            if (!this.Inner.HasSensor(sensor)) {
                return false;
            }
        }
        return true;
    }

    private bool SetMotionSensorsEnabled(bool enabled) {
        foreach (var sensor in motionSensors) {
            this.Inner.SetSensorEnabled(sensor, enabled);
        }
        return true;
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
        SDL.GamepadButton.Misc3, // left trigger
        SDL.GamepadButton.Misc4, // right trigger
        SDL.GamepadButton.COUNT, // home
        SDL.GamepadButton.LeftPaddle1, // left joycon, left shoulder
        SDL.GamepadButton.LeftPaddle2, // left joycon, right shoulder
        SDL.GamepadButton.RightPaddle2, // right joycon, left shoulder
        SDL.GamepadButton.RightPaddle1, // right joycon, right shoulder
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

    // No keyboard support currently implemented, but... keyboard controls are kind of a joke.
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

    private static ushort ConvertMotorStrength(int input, float scale) {
        // For some reason, motorStrengthL and motorStrengthS use the uint8 range of values.
        // Despite the field using an int32.
        // Despite Rewired, the only gamepad bindings actually used by the vanilla game on PC, asking for a float32 in 0..=1.
        // Despite the vibration callback from PS2KatamariSimulation using that very same representation.
        //
        // Technically, since the stereo haptics patch in FramerateUncap is the "other side of this",
        // I *could* make a change both here and there to instead use the full int32 range, or at least a larger fraction of it.
        // However, 256 (255? does 0 really count here?) possible power levels is probably actually plenty, right?

        var n = (float)input;
        // 0xFF becomes 0xFFFF, 0x7F becomes 0x7F7F, etc.
        // Better than my initial `<< 8` approach that resulted in the lower byte being zero, resulting in a small bias towards weaker vibration.
        n *= 0x101;
        n *= scale;
        n = Mathf.Clamp(n, 0f, 65535f);
        return (ushort)n;
    }

    public override void Vibration(float time) {
        if (!this.IsVibration) return;

        var strength = VibrationStrengthOption.Value;
        var low = ConvertMotorStrength(this.motorStrengthL, strength);  // "L(ong)" wavelength = "low" frequency ("left" motor, practically)
        var high = ConvertMotorStrength(this.motorStrengthS, strength); // "S(hort)" wavelength = "high" frequency ("right" motor, practically)
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
        if (!this.Enabled || this.inner == null) return;

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

        /*
        if (this.PadMode == PadMode.Dual) {
            this.TickMotion();
        }
        */
    }

    private void TickMotion() {
        this.Inner.GetSensorData(SDL.SensorType.AccelLeft, out var l);
        this.Inner.GetSensorData(SDL.SensorType.AccelRight, out var r);
        const float ratio = 1 / 9.80665f; // convert from meters/sec² to g-force
        l *= ratio;
        r *= ratio;
        this.PushSensorAccelerationLeft(r.x, r.z, r.y); // no idea why they're supposed to get swapped
        this.PushSensorAccelerationRight(l.x, l.z, l.y);
    }
}
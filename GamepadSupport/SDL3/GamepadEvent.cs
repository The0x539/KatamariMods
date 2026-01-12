using System.Runtime.InteropServices;

namespace GamepadSupport.SDL3;

[StructLayout(LayoutKind.Sequential)]
public struct GamepadAxisEvent {
    public CommonEvent common;
    public JoystickID which;
    public byte axis;
    private readonly byte padding1, padding2, padding3;
    public short value;
    private readonly short padding4;

    public readonly GamepadAxis Axis => (GamepadAxis)this.axis;
}

[StructLayout(LayoutKind.Sequential)]
public struct GamepadButtonEvent {
    public CommonEvent common;
    public JoystickID which;
    public byte button;
    public bool down; // TODO: confirm that this takes up 1 byte and not 4
    private readonly byte padding1, padding2;

    public readonly GamepadButton Button => (GamepadButton)this.button;
}

[StructLayout(LayoutKind.Sequential)]
public struct GamepadDeviceEvent {
    public CommonEvent common;
    public JoystickID which;
}

[StructLayout(LayoutKind.Sequential)]
public struct GamepadSensorEvent {
    public CommonEvent common;
    public JoystickID which;
    public int sensor;
    public float x, y, z;
    public ulong sensorTimestamp;

    public readonly SensorType SensorType => (SensorType)this.sensor;
}

[StructLayout(LayoutKind.Sequential)]
public struct GamepadTouchpadEvent {
    public CommonEvent common;
    public JoystickID which;
    public int touchpad, finger;
    public float x, y, pressure;
}
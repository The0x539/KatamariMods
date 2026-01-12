using System;
using System.Runtime.InteropServices;

namespace GamepadSupport.SDL3;

internal partial struct RawGamepad { }
public enum JoystickID : uint;

public unsafe class Gamepad : IDisposable {
    private readonly RawGamepad* ptr;
    private bool isDisposed;

    public static unsafe SDLArray<JoystickID> GetGamepads() {
        int count;
        var ptr = RawBindings.SDL_GetGamepads(&count);
        return new(ptr, count);
    }

    public Gamepad(JoystickID instanceId) {
        this.ptr = RawBindings.SDL_OpenGamepad(instanceId);
    }

    ~Gamepad() => this.Dispose();
    public void Dispose() {
        if (this.isDisposed) return;
        this.isDisposed = true;
        RawBindings.SDL_CloseGamepad(this.ptr);
    }

    public bool IsConnected => RawBindings.SDL_GamepadConnected(this.ptr);

    public static string NameForID(JoystickID instanceId) => ConvertName(RawBindings.SDL_GetGamepadNameForID(instanceId));
    public static int PlayerIndexForID(JoystickID instanceId) => RawBindings.SDL_GetGamepadPlayerIndexForID(instanceId);

    private static string ConvertName(byte* ptr) {
        if (ptr == null) return null;
        // Frustratingly, PtrToStringUTF8 is not available on this framework version.
        // Everything just sucks.
        var name = Marshal.PtrToStringAnsi((nint)ptr);
        return string.Intern(name);
    }

    private string GetName() => ConvertName(RawBindings.SDL_GetGamepadName(this.ptr));

    private string _name = null;
    public string Name => this._name ??= this.GetName();

    private GamepadType? _gamepadType = null;
    public GamepadType GamepadType => this._gamepadType ??= RawBindings.SDL_GetGamepadType(this.ptr);

    public short GetAxis(GamepadAxis axis) => RawBindings.SDL_GetGamepadAxis(this.ptr, axis);
    public bool GetButton(GamepadButton button) => RawBindings.SDL_GetGamepadButton(this.ptr, button);
    public void Rumble(ushort lo, ushort hi, uint duration) => RawBindings.SDL_RumbleGamepad(this.ptr, lo, hi, duration).ThrowIfFalse();

    public int PlayerIndex {
        get => RawBindings.SDL_GetGamepadPlayerIndex(this.ptr);
        set => RawBindings.SDL_SetGamepadPlayerIndex(this.ptr, value);
    }
}
using System.Runtime.InteropServices;

namespace GamepadSupport.SDL3;

internal partial struct RawGamepad { }
public enum JoystickID : uint;

public unsafe class Gamepad {
    private readonly JoystickID instanceId;
    private readonly RawGamepad* ptr;

    public static unsafe SDLArray<JoystickID> GetGamepads() {
        int count;
        var ptr = RawBindings.SDL_GetGamepads(&count);
        return new(ptr, count);
    }

    public Gamepad(JoystickID instanceId) {
        this.instanceId = instanceId;
        this.ptr = RawBindings.SDL_OpenGamepad(instanceId);
    }

    public static void Update() {
        RawBindings.SDL_UpdateGamepads();
    }

    public bool IsConnected => RawBindings.SDL_GamepadConnected(this.ptr);

    private string GetName() {
        var ptr = RawBindings.SDL_GetGamepadName(this.ptr);
        if (ptr == null) return null;
        // Frustratingly, PtrToStringUTF8 is not available on this framework version.
        // Everything just sucks.
        var name = Marshal.PtrToStringAnsi((nint)ptr);
        SDL.Free(ptr);
        return string.Intern(name);
    }

    private string _name = null;
    public string Name => this._name ??= this.GetName();

    private GamepadType? _gamepadType = null;
    public GamepadType GamepadType => this._gamepadType ??= RawBindings.SDL_GetGamepadType(this.ptr);

    public short GetAxis(GamepadAxis axis) => RawBindings.SDL_GetGamepadAxis(this.ptr, axis);
    public bool GetButton(GamepadButton button) => RawBindings.SDL_GetGamepadButton(this.ptr, button);
    public bool Rumble(ushort lo, ushort hi, uint duration) => RawBindings.SDL_RumbleGamepad(lo, hi, duration);
}
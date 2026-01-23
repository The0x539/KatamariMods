using System;
using System.Runtime.InteropServices;

namespace GamepadSupport.SDL3;

public static unsafe partial class SDL {
    public static void Free(void* mem) => RawBindings.SDL_free(mem);
    public static bool InitSubSystem(InitFlags flags) => RawBindings.SDL_InitSubSystem(flags);
    public static string GetError() => Marshal.PtrToStringAnsi((nint)RawBindings.SDL_GetError());
}

internal static unsafe partial class RawBindings {
    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_InitSubSystem(InitFlags flags);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SDL_free(void* mem);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* SDL_malloc(nuint size);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* SDL_calloc(nuint nmemb, nuint size);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern byte* SDL_GetError();

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SDL_ClearError();

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_PollEvent(void* ev);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern JoystickID* SDL_GetGamepads(int* count);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static unsafe extern byte** SDL_GetGamepadMappings(int* count);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern JoystickID* SDL_GetJoysticks(int* count);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern RawGamepad* SDL_OpenGamepad(JoystickID instance_id);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SDL_CloseGamepad(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_GamepadConnected(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern byte* SDL_GetGamepadName(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern byte* SDL_GetGamepadNameForID(JoystickID instanceId);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern GamepadType SDL_GetGamepadType(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern short SDL_GetGamepadAxis(RawGamepad* gamepad, GamepadAxis axis);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_GetGamepadButton(RawGamepad* gamepad, GamepadButton button);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SDL_UpdateGamepads();

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SDL_UpdateJoysticks();

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_RumbleGamepad(RawGamepad* gamepad, ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int SDL_GetGamepadPlayerIndex(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int SDL_GetGamepadPlayerIndexForID(JoystickID instanceId);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern SDLBool SDL_SetGamepadPlayerIndex(RawGamepad* gamepad, int playerIndex);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern JoystickID SDL_GetGamepadID(RawGamepad* gamepad);

    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern RawGamepad* SDL_GetGamepadFromPlayerIndex(int playerIndex);
}

[Flags]
public enum InitFlags : uint {
    Audio = 0x0000_0010U,
    Video = 0x0000_0020U,
    Joystick = 0x0000_0200U,
    Haptic = 0x0000_1000U,
    Gamepad = 0x0000_2000U,
    Events = 0x0000_4000U,
    Sensor = 0x0000_8000U,
    Camera = 0x0001_0000U,
}

public readonly record struct SDLBool {
    private readonly byte value;

    internal const byte FALSE_VALUE = 0;
    internal const byte TRUE_VALUE = 1;

    [Obsolete("Never explicitly construct an SDL bool.")]
    public SDLBool() { }

    internal SDLBool(byte value) {
        this.value = value;
    }

    public static implicit operator bool(SDLBool b) => b.value != FALSE_VALUE;
    public static implicit operator SDLBool(bool b) => new(b ? TRUE_VALUE : FALSE_VALUE);

    public bool Equals(SDLBool other) => (bool)other == (bool)this;
    public override int GetHashCode() => ((bool)this).GetHashCode();

    public readonly void ThrowIfFalse() => SDLException.ThrowIfFalse(this);
}

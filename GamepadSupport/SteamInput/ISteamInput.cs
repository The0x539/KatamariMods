using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GamepadSupport.SteamInput;

public readonly unsafe struct ISteamInput {
    private readonly void* ptr;

    public static readonly ISteamInput Instance = SteamAPI_SteamInput_v006();

    [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
    private static extern ISteamInput SteamAPI_SteamInput_v006();

    public InputHandle[] GetConnectedControllers() {
        var handles = new InputHandle[16];
        var length = this.GetConnectedControllers(handles);
        return handles.Take(length).ToArray();
    }
}

internal static class ISteamInputBindings {
    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_Init", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Init(this ISteamInput self, bool bExplicitlyCallRunFrame = false);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_Shutdown", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Shutdown(this ISteamInput self);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_RunFrame", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RunFrame(this ISteamInput self);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_GetGlyphForXboxOrigin", CallingConvention = CallingConvention.Cdecl)]
    internal static extern LPUtf8Str GetGlyphForXboxOrigin(this ISteamInput self, XboxOrigin eOrigin);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_GetGlyphPNGForActionOrigin", CallingConvention = CallingConvention.Cdecl)]
    internal static extern LPUtf8Str GetGlyphPNGForActionOrigin(this ISteamInput self, ActionOrigin eOrigin, GlyphSize eSize, uint unFlags);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_GetActionOriginFromXboxOrigin", CallingConvention = CallingConvention.Cdecl)]
    internal static extern ActionOrigin GetActionOriginFromXboxOrigin(this ISteamInput self, InputHandle handle, XboxOrigin eOrigin);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_GetInputTypeForHandle", CallingConvention = CallingConvention.Cdecl)]
    internal static extern InputType GetInputTypeForHandle(this ISteamInput self, InputHandle handle);

    [DllImport("steam_api64", EntryPoint = "SteamAPI_ISteamInput_GetConnectedControllers", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int GetConnectedControllers(this ISteamInput self, [In, Out] InputHandle[] handles);
}

internal readonly unsafe struct LPUtf8Str {
    private readonly byte* ptr;

    public unsafe readonly override string ToString() {
        if (this.ptr == null) return "";

        int len;
        for (len = 0; this.ptr[len] != 0; len++) ;

        var utf8 = new byte[len];
        Marshal.Copy((nint)this.ptr, utf8, 0, len);
        return Encoding.UTF8.GetString(utf8);

    }

    public unsafe static implicit operator string(LPUtf8Str p) => p.ToString();
}
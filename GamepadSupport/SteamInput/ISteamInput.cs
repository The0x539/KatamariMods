using System.Runtime.InteropServices;

namespace GamepadSupport.SteamInput;

public struct ISteamInput {
    private readonly ulong handle;

    internal ISteamInput(ulong handle) => this.handle = handle;

    [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern string SteamAPI_ISteamInput_GetGlyphForXboxOrigin(ulong self, XboxOrigin eOrigin);

    public string GetGlyphForXboxOrigin(XboxOrigin eOrigin) => SteamAPI_ISteamInput_GetGlyphForXboxOrigin(this.handle, eOrigin);

    [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SteamAPI_ISteamInput_GetActionOriginFromXboxOrigin(ulong self, XboxOrigin eOrigin);

    public int GetActionOriginFromXboxOrigin(XboxOrigin eOrigin) => SteamAPI_ISteamInput_GetActionOriginFromXboxOrigin(this.handle, eOrigin);
}


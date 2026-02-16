namespace GamepadSupport.SteamInput;

public readonly struct InputHandle {
    public readonly ulong handle;
    public InputHandle(ulong handle) => this.handle = handle;

    public readonly ActionOrigin GetActionOriginFromXboxOrigin(XboxOrigin eOrigin) => ISteamInput.Instance.GetActionOriginFromXboxOrigin(this, eOrigin);

    public readonly InputType InputType => ISteamInput.Instance.GetInputTypeForHandle(this);
}

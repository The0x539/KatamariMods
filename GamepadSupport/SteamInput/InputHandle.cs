namespace GamepadSupport.SteamInput;

public readonly struct InputHandle {
    private readonly ulong handle;
    public InputHandle(ulong handle) => this.handle = handle;

    public bool IsValid => this.handle != 0;

    public readonly ActionOrigin GetActionOriginFromXboxOrigin(XboxOrigin eOrigin) => ISteamInput.Instance.GetActionOriginFromXboxOrigin(this, eOrigin);

    public readonly InputType InputType => ISteamInput.Instance.GetInputTypeForHandle(this);
}

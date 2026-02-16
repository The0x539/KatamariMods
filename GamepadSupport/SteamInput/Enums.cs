namespace GamepadSupport.SteamInput;

public enum XboxOrigin : int {
    A, B, X, Y,
    LeftBumper, RightBumper,
    Menu, // Start
    View, // Back 
    LeftTriggerPull, LeftTriggerClick,
    RightTriggerPull, RightTriggerClick,
    LeftStickMove, LeftStickClick,
    LeftStickNorth, LeftStickSouth, LeftStickWest, LeftStickEast,
    RightStickMove, RightStickClick,
    RightStickNorth, RightStickSouth, RightStickWest, RightStickEast,
    DPadNorth, DPadSouth, DPadWest, DPadEast,
    COUNT,
}

public enum ActionOrigin : int {
    None,
    // I am not writing all of the members. I'm not going to use them.
}

public enum InputType : int {
    Unknown,
    SteamController,
    Xbox360,
    XboxOne,
    Generic, // DirectInput
    PS4,
    AppleMFi,
    Android,
    SwitchJoyConPair,
    SwitchJoyConSingle,
    SwitchPro,
    MobileTouch,
    PS3,
    PS5,
    SteamDeck,
    COUNT,
}

public enum GlyphSize : int {
    Small,
    Medium,
    Large,
    COUNT,
}
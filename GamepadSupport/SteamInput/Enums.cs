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
    A = 333,  // treating the "Steam Deck" group as the generic default
    B, X, Y,
    L1, R1,
    Menu, View,
    LeftPadTouch, LeftPadSwipe, LeftPadClick,
    LeftPadNorth, LeftPadSouth, LeftPadWest, LeftPadEast,
    RightPadTouch, RightPadSwipe, RightPadClick,
    RightPadNorth, RightPadSouth, RightPadWest, RightPadEast,
    L2Soft, L2, R2Soft, R2,
    LeftStickMove, L3,
    LeftStickNorth, LeftStickSouth, LeftStickWest, LeftStickEast,
    LeftStickTouch,
    RightStickMove, R3,
    RightStickNorth, RightStickSouth, RightStickWest, RightStickEast,
    RightStickTouch,
    L4, R4, L5, R5,
    DPadMove,
    DPadNorth, DPadSouth, DPadWest, DPadEast,
    GyroMove, GyroPitch, GyroYaw, GyroRoll,

    PS4_X = 50, PS4_Circle, PS4_Triangle, PS4_Square,
    PS5_X = 258, PS5_Circle, PS5_Triangle, PS5_Square,
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

// Pick one base style and 
public enum GlyphStyle : uint {
    // White shapes with transparent cutout detail.
    Knockout = 0x0,
    // White shapes with black detail.
    Light = 0x1,
    // Black shapes with white detail.
    Dark = 0x2,

    // Use monochrome white/black icons like for other inputs, instead of matching device colors.
    NeutralFaceButtons = 0x10,
    // Use an "outlined style" for face buttons. Exact meaning of this depends on the other options.
    OutlinedFaceButtons = 0x20,
}

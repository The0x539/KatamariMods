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
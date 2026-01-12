namespace GamepadSupport.SDL3;

public enum GamepadButton {
    INVALID = -1,
    South,
    East,
    West,
    North,
    Back,
    Guide,
    Start,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,
    Misc1,
    RightPaddle1,
    LeftPaddle1,
    RightPaddle2,
    LeftPaddle2,
    Touchpad,
    Misc2,
    Misc3,
    Misc4,
    Misc5,
    Misc6,
    COUNT,
}

public enum GamepadAxis : int {
    INVALID = -1,
    LeftX,
    LeftY,
    RightX,
    RightY,
    LeftTrigger,
    RightTrigger,
    COUNT,
}

public enum GamepadBindingType {
    None = 0,
    Button,
    Axis,
    Hat,
}

public enum GamepadType : uint {
    Unknown = 0,
    Standard,
    Xbox360,
    XboxOne,
    PS3,
    PS4,
    PS5,
    SwitchPro,
    SwitchJoyConLeft,
    SwitchJoyConRight,
    SwitchJoyConPair,
    GameCube,
    COUNT,
}

public enum SensorType : int {
    INVALID = -1,
    Unknown,
    Accel,
    Gyro,
    AccelLeft,
    GyroLeft,
    AccelRight,
    GyroRight,
}
using System.Runtime.InteropServices;

namespace GamepadSupport.SDL3;

public partial struct GamepadBinding {
    public GamepadBindingType input_type;

    public _input_e__Union input;

    public GamepadBindingType output_type;

    public _output_e__Union output;

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _input_e__Union {
        [FieldOffset(0)]
        public int button;

        [FieldOffset(0)]
        public _axis_e__Struct axis;

        [FieldOffset(0)]
        public _hat_e__Struct hat;

        public partial struct _axis_e__Struct {
            public int axis;
            public int axis_min;
            public int axis_max;
        }

        public partial struct _hat_e__Struct {
            public int hat;
            public int hat_mask;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _output_e__Union {
        [FieldOffset(0)]
        public GamepadButton button;

        [FieldOffset(0)]
        public _axis_e__Struct axis;

        public partial struct _axis_e__Struct {
            public GamepadAxis axis;

            public int axis_min;

            public int axis_max;
        }
    }
}

public enum GamepadButton {
    Invalid = -1,
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
    Count,
}

public enum GamepadAxis : int {
    Invalid = -1,
    LeftX,
    LeftY,
    RightX,
    RightY,
    LeftTrigger,
    RightTrigger,
    Count,
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
    Count,
}

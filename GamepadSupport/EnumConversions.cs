using GamepadSupport.SteamInput;

using MyGame.InputStatus;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

public static class EnumConversions {
    public static XboxOrigin ToXboxOrigin(this KeyMap k, bool japaneseFaceButtons) => k switch {
        KeyMap.A => XboxOrigin.A,
        KeyMap.B => XboxOrigin.B,
        KeyMap.X => XboxOrigin.X,
        KeyMap.Y => XboxOrigin.Y,
        KeyMap.L1 => XboxOrigin.LeftBumper,
        KeyMap.R1 => XboxOrigin.RightBumper,
        KeyMap.Back => XboxOrigin.View,
        KeyMap.Start => XboxOrigin.Menu,
        KeyMap.L3 => XboxOrigin.LeftStickClick,
        KeyMap.R3 => XboxOrigin.RightStickClick,
        KeyMap.L2 => XboxOrigin.LeftTriggerClick,
        KeyMap.R2 => XboxOrigin.RightTriggerClick,
        KeyMap.Home => XboxOrigin.Menu, // ¯\_(ツ)_/¯
        KeyMap.Left => XboxOrigin.DPadWest,
        KeyMap.Right => XboxOrigin.DPadEast,
        KeyMap.Up => XboxOrigin.DPadNorth,
        KeyMap.Down => XboxOrigin.DPadSouth,
        KeyMap.StickLeftLeft => XboxOrigin.LeftStickWest,
        KeyMap.StickLeftRight => XboxOrigin.LeftStickEast,
        KeyMap.StickLeftUp => XboxOrigin.LeftStickNorth,
        KeyMap.StickLeftDown => XboxOrigin.LeftStickSouth,
        KeyMap.StickRightLeft => XboxOrigin.RightStickWest,
        KeyMap.StickRightRight => XboxOrigin.RightStickEast,
        KeyMap.StickRightUp => XboxOrigin.RightStickNorth,
        KeyMap.StickRightDown => XboxOrigin.RightStickSouth,

        KeyMap.Enter => japaneseFaceButtons ? XboxOrigin.B : XboxOrigin.A,
        KeyMap.Cancel => japaneseFaceButtons ? XboxOrigin.A : XboxOrigin.B,

        _ => XboxOrigin.Menu, // idk
    };

    public static ActionOrigin ToActionOrigin(this XboxOrigin x) => x switch {
        XboxOrigin.A => ActionOrigin.A,
        XboxOrigin.B => ActionOrigin.B,
        XboxOrigin.X => ActionOrigin.X,
        XboxOrigin.Y => ActionOrigin.Y,
        XboxOrigin.LeftBumper => ActionOrigin.L1,
        XboxOrigin.RightBumper => ActionOrigin.R1,
        XboxOrigin.Menu => ActionOrigin.Menu,
        XboxOrigin.View => ActionOrigin.View,
        XboxOrigin.LeftTriggerPull => ActionOrigin.L2Soft,
        XboxOrigin.RightTriggerPull => ActionOrigin.R2Soft,
        XboxOrigin.LeftTriggerClick => ActionOrigin.L2,
        XboxOrigin.RightTriggerClick => ActionOrigin.R2,
        XboxOrigin.LeftStickMove => ActionOrigin.LeftStickMove,
        XboxOrigin.RightStickMove => ActionOrigin.RightStickMove,
        XboxOrigin.LeftStickClick => ActionOrigin.L3,
        XboxOrigin.RightStickClick => ActionOrigin.R3,
        XboxOrigin.LeftStickNorth => ActionOrigin.LeftStickNorth,
        XboxOrigin.LeftStickSouth => ActionOrigin.LeftStickSouth,
        XboxOrigin.LeftStickEast => ActionOrigin.LeftStickEast,
        XboxOrigin.LeftStickWest => ActionOrigin.LeftStickWest,
        XboxOrigin.RightStickNorth => ActionOrigin.RightStickNorth,
        XboxOrigin.RightStickSouth => ActionOrigin.RightStickSouth,
        XboxOrigin.RightStickEast => ActionOrigin.RightStickEast,
        XboxOrigin.RightStickWest => ActionOrigin.RightStickWest,
        XboxOrigin.DPadNorth => ActionOrigin.DPadNorth,
        XboxOrigin.DPadSouth => ActionOrigin.DPadSouth,
        XboxOrigin.DPadEast => ActionOrigin.DPadEast,
        XboxOrigin.DPadWest => ActionOrigin.DPadWest,
        _ => ActionOrigin.None,
    };

    public static InputType ToSteam(this SDL.GamepadType g) => g switch {
        SDL.GamepadType.Standard => InputType.SteamDeck,
        SDL.GamepadType.Xbox360 => InputType.Xbox360,
        SDL.GamepadType.XboxOne => InputType.XboxOne,
        SDL.GamepadType.PS3 => InputType.PS3,
        SDL.GamepadType.PS4 => InputType.PS4,
        SDL.GamepadType.PS5 => InputType.PS5,
        SDL.GamepadType.SwitchPro => InputType.SwitchPro,
        SDL.GamepadType.SwitchJoyConLeft or SDL.GamepadType.SwitchJoyConRight => InputType.SwitchJoyConSingle,
        SDL.GamepadType.SwitchJoyConPair => InputType.SwitchJoyConPair,
        SDL.GamepadType.GameCube => InputType.Unknown, // Hoping Steam adds a GameCube controller type in a newer SDK
        _ => InputType.Unknown,
    };
}

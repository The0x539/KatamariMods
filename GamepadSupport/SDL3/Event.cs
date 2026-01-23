namespace GamepadSupport.SDL3;

#pragma warning disable CS8500 // the target runtime doesn't know what "unmanaged" is

public unsafe class SDLEvent {
    private readonly CommonEvent* buffer;

    public SDLEvent() => this.buffer = (CommonEvent*)RawBindings.SDL_calloc(1, 128);
    ~SDLEvent() => SDL.Free(this.buffer);

    public static SDLEvent? Poll() {
        var ev = new SDLEvent();
        bool gotEvent = RawBindings.SDL_PollEvent(ev.buffer);
        return gotEvent ? ev : null;
    }

    public uint RawEventType => *(uint*)this.buffer;
    public EventType EventType => *(EventType*)this.buffer;

    public CommonEvent AsCommon() => *this.buffer;

    private T As<T>() where T : struct => *(T*)this.buffer;

    public object Dispatch() => this.EventType switch {
        EventType.GamepadAdded or
        EventType.GamepadRemoved or
        EventType.GamepadRemapped or
        EventType.GamepadUpdateComplete or
        EventType.GamepadSteamHandleUpdated => this.As<GamepadDeviceEvent>(),

        EventType.GamepadTouchpadDown or
        EventType.GamepadTouchpadMotion or
        EventType.GamepadTouchpadMotion => this.As<GamepadTouchpadEvent>(),

        EventType.GamepadAxisMotion => this.As<GamepadAxisEvent>(),
        EventType.GamepadSensorUpdate => this.As<GamepadSensorEvent>(),
        EventType.GamepadButtonDown or EventType.GamepadButtonUp => this.As<GamepadButtonEvent>(),

        _ => this.AsCommon(),
    };
}

public struct CommonEvent {
    public EventType type;
    public uint reserved;
    public ulong timestamp;
}

public enum EventType : uint {
    FIRST = 0,

    Quit = 0x100,
    Terminating,
    LowMemory,
    WillEnterBackground,
    DidEnterBackground,
    WillEnterForeground,
    DidEnterForeground,
    LocaleChanged,
    SystemThemeChanged,

    DisplayOrientation = 0x151,
    DisplayAdded,
    DisplayRemoved,
    DisplayMoved,
    DisplayDesktopModeChanged,
    DisplayCurrentModeChanged,
    DisplayContentScaleChanged,
    DisplayUsableBoundsChanged,
    DISPLAY_FIRST = DisplayOrientation,
    DISPLAY_LAST = DisplayUsableBoundsChanged,

    WindowShown = 0x202,
    WindowHidden,
    WindowExposed,
    WindowMoved,
    WindowResized,
    WindowPixelSizeChanged,
    WindowMetalViewResized,
    WindowMinimized,
    WindowMaximized,
    WindowRestored,
    WindowMouseEnter,
    WindowMouseLeave,
    WindowFocusGained,
    WindowFocusLost,
    WindowCloseRequested,
    WindowHitTest,
    WindowIccprofChanged,
    WindowDisplayChanged,
    WindowDisplayScaleChanged,
    WindowSafeAreaChanged,
    WindowOccluded,
    WindowEnterFullscreen,
    WindowLeaveFullscreen,
    WindowDestroyed,
    WindowHdrStateChanged,
    WINDOW_FIRST = WindowShown,
    WINDOW_LAST = WindowHdrStateChanged,

    KeyDown = 0x300,
    KeyUp,
    TextEditing,
    TextInput,
    KeymapChanged,
    KeyboardAdded,
    KeyboardRemoved,
    TextEditingCandidates,
    ScreenKeyboardShown,
    ScreenKeyboardHidden,

    MouseMotion = 0x400,
    MouseButtonDown,
    MouseButtonUp,
    MouseWheel,
    MouseAdded,
    MouseRemoved,

    JoystickAxisMotion = 0x600,
    JoystickBallMotion,
    JoystickHatMotion,
    JoystickButtonDown,
    JoystickButtonUp,
    JoystickAdded,
    JoystickRemoved,
    JoystickBatteryUpdated,
    JoystickUpdateComplete,

    GamepadAxisMotion = 0x650,
    GamepadButtonDown,
    GamepadButtonUp,
    GamepadAdded,
    GamepadRemoved,
    GamepadRemapped,
    GamepadTouchpadDown,
    GamepadTouchpadMotion,
    GamepadTouchpadUp,
    GamepadSensorUpdate,
    GamepadUpdateComplete,
    GamepadSteamHandleUpdated,

    FingerDown = 0x700,
    FingerUp,
    FingerMotion,
    FingerCanceled,

    PinchBegin = 0x710,
    PinchUpdate,
    PinchEnd,

    ClipboardUpdate = 0x900,

    DropFile = 0x1000,
    DropText,
    DropBegin,
    DropComplete,
    DropPosition,

    AudioDeviceAdded = 0x1100,
    AudioDeviceRemoved,
    AudioDeviceFormatChanged,

    SensorUpdate = 0x1200,

    PenProximityIn = 0x1300,
    PenProximityOut,
    PenDown,
    PenUp,
    PenButtonDown,
    PenButtonUp,
    PenMotion,
    PenAxis,

    CameraDeviceAdded = 0x1400,
    CameraDeviceRemoved,
    CameraDeviceApproved,
    CameraDeviceDenied,

    RenderTargetsReset = 0x2000,
    RenderDeviceReset,
    RenderDeviceLost,

    Private0 = 0x4000,
    Private1,
    Private2,
    Private3,

    POLL_SENTINEL = 0x7F00,
    USER = 0x8000,
    LAST = 0xFFFF,
}
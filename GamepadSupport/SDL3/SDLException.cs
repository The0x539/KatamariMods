using System;

namespace GamepadSupport.SDL3;

public class SDLException(string message) : Exception(message) {
    public static void ThrowIfFalse(bool success) {
        if (!success) {
            throw new SDLException(SDL.GetError());
        }
    }
}

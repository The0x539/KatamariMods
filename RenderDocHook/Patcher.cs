using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RenderDocHook;

public static class Patcher {
    static Patcher() {
        unsafe {
            var options = new CaptureOptions();
            RenderDoc.SetCaptureOptions(options);
            //RenderDoc.SetCaptureFile("./renderdoc.cap");
            //RenderDoc.SetDebugLogFile("./renderdoc.debug.log");
        }
    }

    // Needed for the BepInEx 5 preloader to detect this assembly as something worth running
    public static IEnumerable<string> TargetDLLs => [];
    public static void Patch(Mono.Cecil.AssemblyDefinition assembly) { }
}

[StructLayout(LayoutKind.Sequential, Size = 20)]
internal struct CaptureOptions {
    public CaptureOptions() { }

    public bool allowVSync = true;
    public bool allowFullscreen = true;
    public bool apiValidation = false;
    public bool captureCallstacks = false;
    public bool captureCallstacksOnlyActions = false;
    public uint delayForDebugger = 0;
    public bool verifyBufferAccess = false;
    public bool hookIntoChildren = false;
    public bool refAllResources = false;
    public bool captureAllCmdLists = false;
    public bool debugOutputMute = true;
    public uint softMemoryLimit = 0;
}

internal static class RenderDoc {
    // TODO: Determine if we can get away with omitting the explicit writing of Cdecl
    [DllImport("renderdoc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "INTERNAL_SetCaptureOptions")]
    public static unsafe extern void SetCaptureOptions(in CaptureOptions options);

    [DllImport("renderdoc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "INTERNAL_SetCaptureFile")]
    public static unsafe extern void SetCaptureFile([MarshalAs(UnmanagedType.LPStr)] string capfile);

    [DllImport("renderdoc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "INTERNAL_SetDebugLogFile")]
    public static unsafe extern void SetDebugLogFile([MarshalAs(UnmanagedType.LPStr)] string logfile);
}
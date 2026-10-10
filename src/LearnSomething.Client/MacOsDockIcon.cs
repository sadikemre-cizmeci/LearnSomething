using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LearnSomething.Client;

// Sets the Dock icon at runtime via a direct AppKit call. This app isn't
// (yet) packaged as a real .app bundle, and Avalonia's own Window.Icon is
// a title-bar/task-switcher icon on Windows/Linux with no confirmed effect
// on the macOS Dock tile for an unbundled `dotnet run` process — which
// matches the reported blank Dock icon even with a valid file set via
// Icon=. This is the same technique Java's `-Xdock:icon=` flag uses under
// the hood for unbundled JVM processes. Same P/Invoke pattern as the
// (since-removed) activation-policy call from the tray-icon rounds.
[SupportedOSPlatform("macos")]
public static class MacOsDockIcon
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    public static void SetFromFile(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"[LearnSomething] Dock icon dosyası bulunamadı: {imagePath}");
                return;
            }

            // objc_getClass only finds classes from frameworks already loaded
            // into the process; don't rely on Avalonia's native library having
            // pulled AppKit in first.
            NativeLibrary.TryLoad("/System/Library/Frameworks/Foundation.framework/Foundation", out _);
            NativeLibrary.TryLoad("/System/Library/Frameworks/AppKit.framework/AppKit", out _);

            var nsStringClass = objc_getClass("NSString");
            var nsImageClass = objc_getClass("NSImage");
            var nsApplicationClass = objc_getClass("NSApplication");

            var pathPtr = Marshal.StringToCoTaskMemUTF8(imagePath);
            try
            {
                var nsPath = objc_msgSend(nsStringClass, sel_registerName("stringWithUTF8String:"), pathPtr);
                var imageAlloc = objc_msgSend(nsImageClass, sel_registerName("alloc"));
                var nsImage = objc_msgSend(imageAlloc, sel_registerName("initWithContentsOfFile:"), nsPath);

                if (nsImage == IntPtr.Zero)
                {
                    Console.WriteLine("[LearnSomething] Dock icon: NSImage yüklenemedi.");
                    return;
                }

                var app = objc_msgSend(nsApplicationClass, sel_registerName("sharedApplication"));
                objc_msgSend(app, sel_registerName("setApplicationIconImage:"), nsImage);
                Console.WriteLine("[LearnSomething] Dock icon ayarlandı.");
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LearnSomething] Dock icon ayarlanamadı: {ex}");
        }
    }
}

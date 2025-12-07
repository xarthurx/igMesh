using System.Runtime.InteropServices;

namespace GSP.Core {
  /// <summary>
  /// Platform detection and native library configuration.
  /// Use these constants and helpers when creating P/Invoke declarations.
  /// </summary>
  public static class Platform {
    /// <summary>
    /// Native library name for Windows.
    /// </summary>
    public const string WindowsLib = "GeoSharPlusCPP.dll";

    /// <summary>
    /// Native library name for macOS.
    /// </summary>
    public const string MacLib = "libGeoSharPlusCPP.dylib";

    /// <summary>
    /// Native library name for Linux.
    /// </summary>
    public const string LinuxLib = "libGeoSharPlusCPP.so";

    /// <summary>
    /// Returns true if running on Windows.
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Returns true if running on macOS.
    /// </summary>
    public static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Returns true if running on Linux.
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets the appropriate native library name for the current platform.
    /// </summary>
    public static string NativeLibrary {
      get {
        if (IsWindows) return WindowsLib;
        if (IsMac) return MacLib;
        if (IsLinux) return LinuxLib;
        throw new PlatformNotSupportedException("Unsupported platform");
      }
    }
  }
}

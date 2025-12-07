using System;
using System.Runtime.InteropServices;

namespace GSP.Core {
  /// <summary>
  /// Cross-platform utilities for marshaling data between C# and native code.
  /// Works on Windows, macOS, and Linux.
  /// </summary>
  public static class MarshalHelper {
    /// <summary>
    /// Copies data from unmanaged memory to a managed byte array and frees the unmanaged memory.
    /// This method handles the common pattern of receiving data from C++ code.
    /// </summary>
    /// <param name="ptr">Pointer to unmanaged memory allocated by native code.</param>
    /// <param name="size">Size of the data in bytes.</param>
    /// <returns>A managed byte array containing the copied data, or empty array if invalid.</returns>
    /// <remarks>
    /// The native code should allocate memory using:
    /// - Windows: CoTaskMemAlloc
    /// - macOS/Linux: malloc (with appropriate .NET runtime mapping)
    /// 
    /// This method uses Marshal.FreeCoTaskMem which works cross-platform in .NET Core/.NET 5+.
    /// </remarks>
    public static byte[] CopyAndFree(IntPtr ptr, int size) {
      if (ptr == IntPtr.Zero || size <= 0)
        return Array.Empty<byte>();

      try {
        var buffer = new byte[size];
        Marshal.Copy(ptr, buffer, 0, size);
        return buffer;
      }
      finally {
        Marshal.FreeCoTaskMem(ptr);
      }
    }

    /// <summary>
    /// Copies data from unmanaged memory without freeing it.
    /// Use when you need to read data but the native side manages the memory.
    /// </summary>
    /// <param name="ptr">Pointer to unmanaged memory.</param>
    /// <param name="size">Size of the data in bytes.</param>
    /// <returns>A managed byte array containing the copied data.</returns>
    public static byte[] Copy(IntPtr ptr, int size) {
      if (ptr == IntPtr.Zero || size <= 0)
        return Array.Empty<byte>();

      var buffer = new byte[size];
      Marshal.Copy(ptr, buffer, 0, size);
      return buffer;
    }

    /// <summary>
    /// Frees memory allocated by native code.
    /// </summary>
    /// <param name="ptr">Pointer to unmanaged memory to free.</param>
    public static void Free(IntPtr ptr) {
      if (ptr != IntPtr.Zero)
        Marshal.FreeCoTaskMem(ptr);
    }
  }
}

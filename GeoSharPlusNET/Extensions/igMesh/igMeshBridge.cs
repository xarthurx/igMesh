// using System;
using System.Runtime.InteropServices;

using GSP.Core;

namespace igMesh.Native {
public static class igMeshBridge {
  // Using GSP.Core.Platform for library names

  // System debugging functions for cross-platform support

#region cross - platform debug
  // Store error messages that can be retrieved by Grasshopper components
  private static readonly List<string> _errorLog = new List<string>();
  private static bool _isNativeLibraryLoaded = false;
  private static string _loadedLibraryPath = string.Empty;
  private static bool _initializationAttempted = false;

  /// <summary>
  /// Returns true if the native library was successfully loaded
  /// </summary>
  public static bool IsNativeLibraryLoaded {
    get {
      if (!_initializationAttempted) {
        InitializeNativeLibrary();
      }
      return _isNativeLibraryLoaded;
    }
  }

  /// <summary>
  /// Path where the library was loaded from (empty if not loaded)
  /// </summary>
  public static string LoadedLibraryPath {
    get {
      if (!_initializationAttempted) {
        InitializeNativeLibrary();
      }
      return _loadedLibraryPath;
    }
  }

  /// <summary>
  /// Get error messages that can be displayed in Grasshopper components
  /// </summary>
  public static string[] GetErrorMessages() {
    lock (_errorLog) {
      return _errorLog.ToArray();
    }
  }

  /// <summary>
  /// Clear the error log
  /// </summary>
  public static void ClearErrorLog() {
    lock (_errorLog) {
      _errorLog.Clear();
    }
  }

  /// <summary>
  /// Add an error message to the log
  /// </summary>
  private static void LogError(string message) {
    lock (_errorLog) {
      _errorLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

      // Keep log at a reasonable size
      if (_errorLog.Count > 100)
        _errorLog.RemoveAt(0);
    }
  }

  // P/Invoke declarations for macOS dynamic library loading
  [DllImport("libdl.dylib", EntryPoint = "dlopen")]
  private static extern IntPtr dlopen_mac(string path, int flags);

  [DllImport("libdl.dylib", EntryPoint = "dlerror")]
  private static extern IntPtr dlerror_mac();

  private static string dlerror() {
    IntPtr ptr = dlerror_mac();
    if (ptr == IntPtr.Zero)
      return string.Empty;
    return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
  }

  /// <summary>
  /// Initialize native library loading - called lazily instead of in static constructor
  /// This prevents the entire assembly from failing to load if the native lib isn't found
  /// </summary>
  private static void InitializeNativeLibrary() {
    if (_initializationAttempted)
      return;

    lock (_errorLog) {
      if (_initializationAttempted)
        return;
      _initializationAttempted = true;

      try {
        if (Platform.IsMac) {
          InitializeMacOSLibrary();
        } else if (Platform.IsWindows) {
          InitializeWindowsLibrary();
        } else {
          LogError("Unsupported operating system. Only Windows and macOS are supported.");
        }
      } catch (Exception ex) {
        LogError($"Exception during native library initialization: {ex.Message}");
        LogError($"Stack trace: {ex.StackTrace}");
      }
    }
  }

  private static void InitializeMacOSLibrary() {
    try {
      // List all possible library locations to try
      var searchLocations = new List<string>();

      // 1. Load from assembly directory
      string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
      if (assemblyDirectory != null) {
        searchLocations.Add(Path.Combine(assemblyDirectory, Platform.MacLib));
      }

      // 2. Try parent directory (sometimes needed for GH plugins)
      if (assemblyDirectory != null) {
        string? parentDir = Path.GetDirectoryName(assemblyDirectory);
        if (parentDir != null) {
          searchLocations.Add(Path.Combine(parentDir, Platform.MacLib));
        }
      }

      // 3. Try current directory
      searchLocations.Add(Path.Combine(Directory.GetCurrentDirectory(), Platform.MacLib));

      // 4. Add standard system locations
      searchLocations.Add(Platform.MacLib);  // Default system search paths

      LogError($"Searching for {Platform.MacLib} in the following locations:");
      foreach (var path in searchLocations) {
        LogError($"- {path} (exists: {File.Exists(path)})");
      }

      // Try to load from each location
      IntPtr handle = IntPtr.Zero;
      foreach (var libraryPath in searchLocations) {
        if (File.Exists(libraryPath)) {
          LogError($"Attempting to load native library from: {libraryPath}");

          try {
            handle = dlopen_mac(libraryPath, 2);  // RTLD_NOW = 2

            if (handle != IntPtr.Zero) {
              _isNativeLibraryLoaded = true;
              _loadedLibraryPath = libraryPath;
              LogError($"Successfully loaded native library from: {libraryPath}");
              return;
            } else {
              string errorMsg = dlerror();
              LogError($"Failed to load library from {libraryPath}: {errorMsg}");
            }
          } catch (Exception ex) {
            LogError($"Exception loading from {libraryPath}: {ex.Message}");
          }
        }
      }

      LogError(
          $"Failed to load native library from any location. Plugin functionality will be limited.");
    } catch (Exception ex) {
      LogError($"Exception while setting up native library path: {ex.Message}");
      LogError($"Stack trace: {ex.StackTrace}");
    }
  }

  private static void InitializeWindowsLibrary() {
    try {
      // Locate the DLL file for Windows
      string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
      string dllPath = string.Empty;

      if (assemblyDirectory != null) {
        dllPath = Path.Combine(assemblyDirectory, Platform.WindowsLib);
        if (!File.Exists(dllPath)) {
          // Try parent directory
          string? parentDir = Path.GetDirectoryName(assemblyDirectory);
          if (parentDir != null) {
            dllPath = Path.Combine(parentDir, Platform.WindowsLib);
          }
        }
      }

      if (File.Exists(dllPath)) {
        _isNativeLibraryLoaded = true;
        _loadedLibraryPath = dllPath;
        LogError($"Successfully located native library at: {dllPath}");
      } else {
        LogError($"Failed to locate native library {Platform.WindowsLib} in expected locations.");
        LogError($"Searched: {dllPath}");
      }
    } catch (Exception ex) {
      LogError($"Exception while locating native library path: {ex.Message}");
      LogError($"Stack trace: {ex.StackTrace}");
    }
  }

#endregion

  // For each function, we create 3 functions: Windows, macOS implementations, and the public API

#region Example Round Trip Functions
  // Double Array Round Trip
  [DllImport(Platform.WindowsLib,
             EntryPoint = "double_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  DoubleArrayRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  [DllImport(Platform.MacLib,
             EntryPoint = "double_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  DoubleArrayRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  DoubleArrayRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return DoubleArrayRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return DoubleArrayRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Int Array Round Trip
  [DllImport(Platform.WindowsLib,
             EntryPoint = "int_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IntArrayRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  [DllImport(Platform.MacLib,
             EntryPoint = "int_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IntArrayRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IntArrayRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IntArrayRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IntArrayRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Int Pair Array Round Trip
  [DllImport(Platform.WindowsLib,
             EntryPoint = "int_pair_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IntPairArrayRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  [DllImport(Platform.MacLib,
             EntryPoint = "int_pair_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IntPairArrayRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IntPairArrayRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IntPairArrayRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IntPairArrayRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Double Pair Array Round Trip
  [DllImport(Platform.WindowsLib,
             EntryPoint = "double_pair_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  DoublePairArrayRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  [DllImport(Platform.MacLib,
             EntryPoint = "double_pair_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  DoublePairArrayRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  DoublePairArrayRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return DoublePairArrayRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return DoublePairArrayRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Example: Point Round Trip -- Passing a Point3d to C++ and back
  [DllImport(Platform.WindowsLib,
             EntryPoint = "point3d_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  Point3dRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "point3d_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  Point3dRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  public static bool
  Point3dRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return Point3dRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return Point3dRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Example: Point Array Round Trip -- Passing an array of Point3d to C++ and back
  [DllImport(Platform.WindowsLib,
             EntryPoint = "point3d_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  Point3dArrayRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "point3d_array_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  Point3dArrayRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  public static bool
  Point3dArrayRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return Point3dArrayRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return Point3dArrayRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }
  // Mesh Round Trip -- Passing a Mesh to C++ and back
  [DllImport(Platform.WindowsLib,
             EntryPoint = "mesh_roundtrip",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  MeshRoundTripWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(
      Platform.MacLib, EntryPoint = "mesh_roundtrip", CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  MeshRoundTripMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  public static bool
  MeshRoundTrip(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return MeshRoundTripWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return MeshRoundTripMac(inBuffer, inSize, out outBuffer, out outSize);
  }
#endregion

#region IG - MESH Functions

  // Mesh Centroid -- calculates the centroid of a mesh
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_centroid",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  MeshCentroidWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(
      Platform.MacLib, EntryPoint = "IGM_centroid", CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  MeshCentroidMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  MeshCentroid(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return MeshCentroidWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return MeshCentroidMac(inBuffer, inSize, out outBuffer, out outSize);
  }
  // Load Mesh -- basic function to get a mesh from the native library
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_read_triangle_mesh",
             CallingConvention = CallingConvention.Cdecl,
             CharSet = CharSet.Ansi)]
  private static extern bool LoadMeshWin(string fileName, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_read_triangle_mesh",
             CallingConvention = CallingConvention.Cdecl,
             CharSet = CharSet.Ansi)]
  private static extern bool LoadMeshMac(string fileName, out IntPtr outBuffer, out int outSize);
  public static bool LoadMesh(string fileName, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return LoadMeshWin(fileName, out outBuffer, out outSize);
    else
      return LoadMeshMac(fileName, out outBuffer, out outSize);
  }

  // Save Mesh -- basic function to export a mesh to local HDD
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_write_triangle_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool SaveMeshWin(byte[] inBuffer, int inSize, string fileName);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_write_triangle_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool SaveMeshMac(byte[] inBuffer, int inSize, string fileName);
  public static bool SaveMesh(byte[] inBuffer, int inSize, string fileName) {
    if (Platform.IsWindows)
      return SaveMeshWin(inBuffer, inSize, fileName);
    else
      return SaveMeshMac(inBuffer, inSize, fileName);
  }

  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_barycenter",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_barycenterWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(
      Platform.MacLib, EntryPoint = "IGM_barycenter", CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_barycenterMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_barycenter(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_barycenterWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_barycenterMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Vertex Normals
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_vert_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_vert_normalsWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_vert_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_vert_normalsMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_vert_normals(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_vert_normalsWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_vert_normalsMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Face Normals
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_face_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_face_normalsWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_face_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_face_normalsMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_face_normals(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_face_normalsWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_face_normalsMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Corner Normals - note the additional threshold_deg parameter
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_corner_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_corner_normalsWin(
      byte[] inBuffer, int inSize, double threshold_deg, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_corner_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_corner_normalsMac(
      byte[] inBuffer, int inSize, double threshold_deg, out IntPtr outBuffer, out int outSize);

  public static bool IGM_corner_normals(
      byte[] inBuffer, int inSize, double threshold_deg, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_corner_normalsWin(inBuffer, inSize, threshold_deg, out outBuffer, out outSize);
    else
      return IGM_corner_normalsMac(inBuffer, inSize, threshold_deg, out outBuffer, out outSize);
  }

  // Edge Normals - note the multiple output parameters
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_edge_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_edge_normalsWin(byte[] inBuffer,
                                                 int inSize,
                                                 int weightingType,
                                                 out IntPtr obEN,
                                                 out int obsEN,
                                                 out IntPtr obEI,
                                                 out int obsEI,
                                                 out IntPtr obEMAP,
                                                 out int obsEMAP);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_edge_normals",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_edge_normalsMac(byte[] inBuffer,
                                                 int inSize,
                                                 int weightingType,
                                                 out IntPtr obEN,
                                                 out int obsEN,
                                                 out IntPtr obEI,
                                                 out int obsEI,
                                                 out IntPtr obEMAP,
                                                 out int obsEMAP);

  public static bool IGM_edge_normals(byte[] inBuffer,
                                      int inSize,
                                      int weightingType,
                                      out IntPtr obEN,
                                      out int obsEN,
                                      out IntPtr obEI,
                                      out int obsEI,
                                      out IntPtr obEMAP,
                                      out int obsEMAP) {
    if (Platform.IsWindows)
      return IGM_edge_normalsWin(inBuffer,
                                 inSize,
                                 weightingType,
                                 out obEN,
                                 out obsEN,
                                 out obEI,
                                 out obsEI,
                                 out obEMAP,
                                 out obsEMAP);
    else
      return IGM_edge_normalsMac(inBuffer,
                                 inSize,
                                 weightingType,
                                 out obEN,
                                 out obsEN,
                                 out obEI,
                                 out obsEI,
                                 out obEMAP,
                                 out obsEMAP);
  }

  // Vertex-Vertex Adjacency
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_vert_vert_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_vert_vert_adjacencyWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_vert_vert_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_vert_vert_adjacencyMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_vert_vert_adjacency(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_vert_vert_adjacencyWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_vert_vert_adjacencyMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Vertex-Triangle Adjacency
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_vert_tri_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_vert_tri_adjacencyWin(byte[] inBuffer,
                                                       int inSize,
                                                       out IntPtr outBufferVT,
                                                       out int outSizeVT,
                                                       out IntPtr outBufferVTI,
                                                       out int outSizeVTI);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_vert_tri_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_vert_tri_adjacencyMac(byte[] inBuffer,
                                                       int inSize,
                                                       out IntPtr outBufferVT,
                                                       out int outSizeVT,
                                                       out IntPtr outBufferVTI,
                                                       out int outSizeVTI);

  public static bool IGM_vert_tri_adjacency(byte[] inBuffer,
                                            int inSize,
                                            out IntPtr outBufferVT,
                                            out int outSizeVT,
                                            out IntPtr outBufferVTI,
                                            out int outSizeVTI) {
    if (Platform.IsWindows)
      return IGM_vert_tri_adjacencyWin(
          inBuffer, inSize, out outBufferVT, out outSizeVT, out outBufferVTI, out outSizeVTI);
    else
      return IGM_vert_tri_adjacencyMac(
          inBuffer, inSize, out outBufferVT, out outSizeVT, out outBufferVTI, out outSizeVTI);
  }

  // Triangle-Triangle Adjacency
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_tri_tri_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_tri_tri_adjacencyWin(byte[] inBuffer,
                                                      int inSize,
                                                      out IntPtr outBufferTT,
                                                      out int outSizeTT,
                                                      out IntPtr outBufferTTI,
                                                      out int outSizeTTI);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_tri_tri_adjacency",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_tri_tri_adjacencyMac(byte[] inBuffer,
                                                      int inSize,
                                                      out IntPtr outBufferTT,
                                                      out int outSizeTT,
                                                      out IntPtr outBufferTTI,
                                                      out int outSizeTTI);

  public static bool IGM_tri_tri_adjacency(byte[] inBuffer,
                                           int inSize,
                                           out IntPtr outBufferTT,
                                           out int outSizeTT,
                                           out IntPtr outBufferTTI,
                                           out int outSizeTTI) {
    if (Platform.IsWindows)
      return IGM_tri_tri_adjacencyWin(
          inBuffer, inSize, out outBufferTT, out outSizeTT, out outBufferTTI, out outSizeTTI);
    else
      return IGM_tri_tri_adjacencyMac(
          inBuffer, inSize, out outBufferTT, out outSizeTT, out outBufferTTI, out outSizeTTI);
  }

  // Boundary Loop
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_boundary_loop",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_boundary_loopWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_boundary_loop",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_boundary_loopMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_boundary_loop(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_boundary_loopWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_boundary_loopMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Boundary Facet
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_boundary_facet",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_boundary_facetWin(byte[] inBuffer,
                                                   int inSize,
                                                   out IntPtr outBufferEL,
                                                   out int outSizeEL,
                                                   out IntPtr outBufferTL,
                                                   out int outSizeTL);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_boundary_facet",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_boundary_facetMac(byte[] inBuffer,
                                                   int inSize,
                                                   out IntPtr outBufferEL,
                                                   out int outSizeEL,
                                                   out IntPtr outBufferTL,
                                                   out int outSizeTL);

  public static bool IGM_boundary_facet(byte[] inBuffer,
                                        int inSize,
                                        out IntPtr outBufferEL,
                                        out int outSizeEL,
                                        out IntPtr outBufferTL,
                                        out int outSizeTL) {
    if (Platform.IsWindows)
      return IGM_boundary_facetWin(
          inBuffer, inSize, out outBufferEL, out outSizeEL, out outBufferTL, out outSizeTL);
    else
      return IGM_boundary_facetMac(
          inBuffer, inSize, out outBufferEL, out outSizeEL, out outBufferTL, out outSizeTL);
  }

  // Scalar Remap VtoF
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_remap_VtoF",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_remap_VtoFWin(byte[] inBufferMesh,
                                               int inSizeMesh,
                                               byte[] inBufferScalar,
                                               int inSizeScalar,
                                               out IntPtr outBuffer,
                                               out int outSize);
  [DllImport(
      Platform.MacLib, EntryPoint = "IGM_remap_VtoF", CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_remap_VtoFMac(byte[] inBufferMesh,
                                               int inSizeMesh,
                                               byte[] inBufferScalar,
                                               int inSizeScalar,
                                               out IntPtr outBuffer,
                                               out int outSize);

  public static bool IGM_remap_VtoF(byte[] inBufferMesh,
                                    int inSizeMesh,
                                    byte[] inBufferScalar,
                                    int inSizeScalar,
                                    out IntPtr outBuffer,
                                    out int outSize) {
    if (Platform.IsWindows)
      return IGM_remap_VtoFWin(
          inBufferMesh, inSizeMesh, inBufferScalar, inSizeScalar, out outBuffer, out outSize);
    else
      return IGM_remap_VtoFMac(
          inBufferMesh, inSizeMesh, inBufferScalar, inSizeScalar, out outBuffer, out outSize);
  }

  // Scalar Remap FtoV
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_remap_FtoV",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_remap_FtoVWin(byte[] inBufferMesh,
                                               int inSizeMesh,
                                               byte[] inBufferScalar,
                                               int inSizeScalar,
                                               out IntPtr outBuffer,
                                               out int outSize);
  [DllImport(
      Platform.MacLib, EntryPoint = "IGM_remap_FtoV", CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_remap_FtoVMac(byte[] inBufferMesh,
                                               int inSizeMesh,
                                               byte[] inBufferScalar,
                                               int inSizeScalar,
                                               out IntPtr outBuffer,
                                               out int outSize);

  public static bool IGM_remap_FtoV(byte[] inBufferMesh,
                                    int inSizeMesh,
                                    byte[] inBufferScalar,
                                    int inSizeScalar,
                                    out IntPtr outBuffer,
                                    out int outSize) {
    if (Platform.IsWindows)
      return IGM_remap_FtoVWin(
          inBufferMesh, inSizeMesh, inBufferScalar, inSizeScalar, out outBuffer, out outSize);
    else
      return IGM_remap_FtoVMac(
          inBufferMesh, inSizeMesh, inBufferScalar, inSizeScalar, out outBuffer, out outSize);
  }

  // Principal Curvature - note the multiple output parameters
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_principal_curvature",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_principal_curvatureWin(byte[] inBuffer,
                                                        int inSize,
                                                        uint radius,
                                                        out IntPtr obPD1,
                                                        out int obsPD1,
                                                        out IntPtr obPD2,
                                                        out int obsPD2,
                                                        out IntPtr obPV1,
                                                        out int obsPV1,
                                                        out IntPtr obPV2,
                                                        out int obsPV2);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_principal_curvature",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_principal_curvatureMac(byte[] inBuffer,
                                                        int inSize,
                                                        uint radius,
                                                        out IntPtr obPD1,
                                                        out int obsPD1,
                                                        out IntPtr obPD2,
                                                        out int obsPD2,
                                                        out IntPtr obPV1,
                                                        out int obsPV1,
                                                        out IntPtr obPV2,
                                                        out int obsPV2);

  public static bool IGM_principal_curvature(byte[] inBuffer,
                                             int inSize,
                                             uint radius,
                                             out IntPtr obPD1,
                                             out int obsPD1,
                                             out IntPtr obPD2,
                                             out int obsPD2,
                                             out IntPtr obPV1,
                                             out int obsPV1,
                                             out IntPtr obPV2,
                                             out int obsPV2) {
    if (Platform.IsWindows)
      return IGM_principal_curvatureWin(inBuffer,
                                        inSize,
                                        radius,
                                        out obPD1,
                                        out obsPD1,
                                        out obPD2,
                                        out obsPD2,
                                        out obPV1,
                                        out obsPV1,
                                        out obPV2,
                                        out obsPV2);
    else
      return IGM_principal_curvatureMac(inBuffer,
                                        inSize,
                                        radius,
                                        out obPD1,
                                        out obsPD1,
                                        out obPD2,
                                        out obsPD2,
                                        out obPV1,
                                        out obsPV1,
                                        out obPV2,
                                        out obsPV2);
  }

  // Gaussian Curvature
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_gaussian_curvature",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_gaussian_curvatureWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_gaussian_curvature",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_gaussian_curvatureMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_gaussian_curvature(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_gaussian_curvatureWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_gaussian_curvatureMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Fast Winding Number
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_fast_winding_number",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_fast_winding_numberWin(byte[] inBufferMesh,
                                                        int inSizeMesh,
                                                        byte[] inBufferPoints,
                                                        int inSizePoints,
                                                        out IntPtr outBuffer,
                                                        out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_fast_winding_number",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_fast_winding_numberMac(byte[] inBufferMesh,
                                                        int inSizeMesh,
                                                        byte[] inBufferPoints,
                                                        int inSizePoints,
                                                        out IntPtr outBuffer,
                                                        out int outSize);

  public static bool IGM_fast_winding_number(byte[] inBufferMesh,
                                             int inSizeMesh,
                                             byte[] inBufferPoints,
                                             int inSizePoints,
                                             out IntPtr outBuffer,
                                             out int outSize) {
    if (Platform.IsWindows)
      return IGM_fast_winding_numberWin(
          inBufferMesh, inSizeMesh, inBufferPoints, inSizePoints, out outBuffer, out outSize);
    else
      return IGM_fast_winding_numberMac(
          inBufferMesh, inSizeMesh, inBufferPoints, inSizePoints, out outBuffer, out outSize);
  }

  // Signed Distance
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_signed_distance",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_signed_distanceWin(byte[] inBufferMesh,
                                                    int inSizeMesh,
                                                    byte[] inBufferPoints,
                                                    int inSizePoints,
                                                    int signedType,
                                                    out IntPtr outBufferSD,
                                                    out int outSizeSD,
                                                    out IntPtr outBufferFI,
                                                    out int outSizeFI,
                                                    out IntPtr outBufferCP,
                                                    out int outSizeCP);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_signed_distance",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_signed_distanceMac(byte[] inBufferMesh,
                                                    int inSizeMesh,
                                                    byte[] inBufferPoints,
                                                    int inSizePoints,
                                                    int signedType,
                                                    out IntPtr outBufferSD,
                                                    out int outSizeSD,
                                                    out IntPtr outBufferFI,
                                                    out int outSizeFI,
                                                    out IntPtr outBufferCP,
                                                    out int outSizeCP);

  public static bool IGM_signed_distance(byte[] inBufferMesh,
                                         int inSizeMesh,
                                         byte[] inBufferPoints,
                                         int inSizePoints,
                                         int signedType,
                                         out IntPtr outBufferSD,
                                         out int outSizeSD,
                                         out IntPtr outBufferFI,
                                         out int outSizeFI,
                                         out IntPtr outBufferCP,
                                         out int outSizeCP) {
    if (Platform.IsWindows)
      return IGM_signed_distanceWin(inBufferMesh,
                                    inSizeMesh,
                                    inBufferPoints,
                                    inSizePoints,
                                    signedType,
                                    out outBufferSD,
                                    out outSizeSD,
                                    out outBufferFI,
                                    out outSizeFI,
                                    out outBufferCP,
                                    out outSizeCP);
    else
      return IGM_signed_distanceMac(inBufferMesh,
                                    inSizeMesh,
                                    inBufferPoints,
                                    inSizePoints,
                                    signedType,
                                    out outBufferSD,
                                    out outSizeSD,
                                    out outBufferFI,
                                    out outSizeFI,
                                    out outBufferCP,
                                    out outSizeCP);
  }

  // Quad Planarity
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_quad_planarity",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_quad_planarityWin(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_quad_planarity",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_quad_planarityMac(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_quad_planarity(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_quad_planarityWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_quad_planarityMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Planarize Quad Mesh
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_planarize_quad_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_planarize_quad_meshWin(byte[] inBuffer,
                                                        int inSize,
                                                        int maxIter,
                                                        double threshold,
                                                        out IntPtr outBuffer,
                                                        out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_planarize_quad_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_planarize_quad_meshMac(byte[] inBuffer,
                                                        int inSize,
                                                        int maxIter,
                                                        double threshold,
                                                        out IntPtr outBuffer,
                                                        out int outSize);

  public static bool IGM_planarize_quad_mesh(byte[] inBuffer,
                                             int inSize,
                                             int maxIter,
                                             double threshold,
                                             out IntPtr outBuffer,
                                             out int outSize) {
    if (Platform.IsWindows)
      return IGM_planarize_quad_meshWin(
          inBuffer, inSize, maxIter, threshold, out outBuffer, out outSize);
    else
      return IGM_planarize_quad_meshMac(
          inBuffer, inSize, maxIter, threshold, out outBuffer, out outSize);
  }

  // Laplacian Scalar
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_laplacian_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_laplacian_scalarWin(byte[] inBufferMesh,
                                                     int inSizeMesh,
                                                     byte[] inBufferIndices,
                                                     int inSizeIndices,
                                                     byte[] inBufferValues,
                                                     int inSizeValues,
                                                     out IntPtr outBuffer,
                                                     out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_laplacian_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_laplacian_scalarMac(byte[] inBufferMesh,
                                                     int inSizeMesh,
                                                     byte[] inBufferIndices,
                                                     int inSizeIndices,
                                                     byte[] inBufferValues,
                                                     int inSizeValues,
                                                     out IntPtr outBuffer,
                                                     out int outSize);

  public static bool IGM_laplacian_scalar(byte[] inBufferMesh,
                                          int inSizeMesh,
                                          byte[] inBufferIndices,
                                          int inSizeIndices,
                                          byte[] inBufferValues,
                                          int inSizeValues,
                                          out IntPtr outBuffer,
                                          out int outSize) {
    if (Platform.IsWindows)
      return IGM_laplacian_scalarWin(inBufferMesh,
                                     inSizeMesh,
                                     inBufferIndices,
                                     inSizeIndices,
                                     inBufferValues,
                                     inSizeValues,
                                     out outBuffer,
                                     out outSize);
    else
      return IGM_laplacian_scalarMac(inBufferMesh,
                                     inSizeMesh,
                                     inBufferIndices,
                                     inSizeIndices,
                                     inBufferValues,
                                     inSizeValues,
                                     out outBuffer,
                                     out outSize);
  }

  // Harmonic Parametrization
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_param_harmonic",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_param_harmonicWin(byte[] inBuffer, int inSize, int k, out IntPtr outBuffer, out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_param_harmonic",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool
  IGM_param_harmonicMac(byte[] inBuffer, int inSize, int k, out IntPtr outBuffer, out int outSize);

  public static bool
  IGM_param_harmonic(byte[] inBuffer, int inSize, int k, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_param_harmonicWin(inBuffer, inSize, k, out outBuffer, out outSize);
    else
      return IGM_param_harmonicMac(inBuffer, inSize, k, out outBuffer, out outSize);
  }

  // Heat Geodesic Precompute
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_heat_geodesic_precompute",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_heat_geodesic_precomputeWin(byte[] inBuffer,
                                                             int inSize,
                                                             out IntPtr outBuffer,
                                                             out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_heat_geodesic_precompute",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_heat_geodesic_precomputeMac(byte[] inBuffer,
                                                             int inSize,
                                                             out IntPtr outBuffer,
                                                             out int outSize);

  public static bool
  IGM_heat_geodesic_precompute(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize) {
    if (Platform.IsWindows)
      return IGM_heat_geodesic_precomputeWin(inBuffer, inSize, out outBuffer, out outSize);
    else
      return IGM_heat_geodesic_precomputeMac(inBuffer, inSize, out outBuffer, out outSize);
  }

  // Heat Geodesic Solve
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_heat_geodesic_solve",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_heat_geodesic_solveWin(byte[] inBuffer,
                                                        int inSize,
                                                        byte[] inBufferSources,
                                                        int inSizeSources,
                                                        out IntPtr outBuffer,
                                                        out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_heat_geodesic_solve",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_heat_geodesic_solveMac(byte[] inBuffer,
                                                        int inSize,
                                                        byte[] inBufferSources,
                                                        int inSizeSources,
                                                        out IntPtr outBuffer,
                                                        out int outSize);

  public static bool IGM_heat_geodesic_solve(byte[] inBuffer,
                                             int inSize,
                                             byte[] inBufferSources,
                                             int inSizeSources,
                                             out IntPtr outBuffer,
                                             out int outSize) {
    if (Platform.IsWindows)
      return IGM_heat_geodesic_solveWin(
          inBuffer, inSize, inBufferSources, inSizeSources, out outBuffer, out outSize);
    else
      return IGM_heat_geodesic_solveMac(
          inBuffer, inSize, inBufferSources, inSizeSources, out outBuffer, out outSize);
  }

  // Random Points on Mesh
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_random_point_on_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_random_point_on_meshWin(byte[] inBuffer,
                                                         int inSize,
                                                         int N,
                                                         out IntPtr outBuffer,
                                                         out int outSize,
                                                         out IntPtr outBufferFI,
                                                         out int outSizeFI);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_random_point_on_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_random_point_on_meshMac(byte[] inBuffer,
                                                         int inSize,
                                                         int N,
                                                         out IntPtr outBuffer,
                                                         out int outSize,
                                                         out IntPtr outBufferFI,
                                                         out int outSizeFI);

  public static bool IGM_random_point_on_mesh(byte[] inBuffer,
                                              int inSize,
                                              int N,
                                              out IntPtr outBuffer,
                                              out int outSize,
                                              out IntPtr outBufferFI,
                                              out int outSizeFI) {
    if (Platform.IsWindows)
      return IGM_random_point_on_meshWin(
          inBuffer, inSize, N, out outBuffer, out outSize, out outBufferFI, out outSizeFI);
    else
      return IGM_random_point_on_meshMac(
          inBuffer, inSize, N, out outBuffer, out outSize, out outBufferFI, out outSizeFI);
  }

  // Blue Noise Sampling on Mesh
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_blue_noise_sampling_on_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_blue_noise_sampling_on_meshWin(byte[] inBuffer,
                                                                int inSize,
                                                                int N,
                                                                out IntPtr outBuffer,
                                                                out int outSize,
                                                                out IntPtr outBufferFI,
                                                                out int outSizeFI);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_blue_noise_sampling_on_mesh",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_blue_noise_sampling_on_meshMac(byte[] inBuffer,
                                                                int inSize,
                                                                int N,
                                                                out IntPtr outBuffer,
                                                                out int outSize,
                                                                out IntPtr outBufferFI,
                                                                out int outSizeFI);

  public static bool IGM_blue_noise_sampling_on_mesh(byte[] inBuffer,
                                                     int inSize,
                                                     int N,
                                                     out IntPtr outBuffer,
                                                     out int outSize,
                                                     out IntPtr outBufferFI,
                                                     out int outSizeFI) {
    if (Platform.IsWindows)
      return IGM_blue_noise_sampling_on_meshWin(
          inBuffer, inSize, N, out outBuffer, out outSize, out outBufferFI, out outSizeFI);
    else
      return IGM_blue_noise_sampling_on_meshMac(
          inBuffer, inSize, N, out outBuffer, out outSize, out outBufferFI, out outSizeFI);
  }

  // Constrained Scalar (equivalent to IGM_laplacian_scalar - they appear to be the same
  // functionality)
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_constrained_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_constrained_scalarWin(byte[] inBufferMesh,
                                                       int inSizeMesh,
                                                       byte[] inBufferIndices,
                                                       int inSizeIndices,
                                                       byte[] inBufferValues,
                                                       int inSizeValues,
                                                       out IntPtr outBuffer,
                                                       out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_constrained_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_constrained_scalarMac(byte[] inBufferMesh,
                                                       int inSizeMesh,
                                                       byte[] inBufferIndices,
                                                       int inSizeIndices,
                                                       byte[] inBufferValues,
                                                       int inSizeValues,
                                                       out IntPtr outBuffer,
                                                       out int outSize);

  public static bool IGM_constrained_scalar(byte[] inBufferMesh,
                                            int inSizeMesh,
                                            byte[] inBufferIndices,
                                            int inSizeIndices,
                                            byte[] inBufferValues,
                                            int inSizeValues,
                                            out IntPtr outBuffer,
                                            out int outSize) {
    if (Platform.IsWindows)
      return IGM_constrained_scalarWin(inBufferMesh,
                                       inSizeMesh,
                                       inBufferIndices,
                                       inSizeIndices,
                                       inBufferValues,
                                       inSizeValues,
                                       out outBuffer,
                                       out outSize);
    else
      return IGM_constrained_scalarMac(inBufferMesh,
                                       inSizeMesh,
                                       inBufferIndices,
                                       inSizeIndices,
                                       inBufferValues,
                                       inSizeValues,
                                       out outBuffer,
                                       out outSize);
  }

  // Extract Isoline from Scalar
  [DllImport(Platform.WindowsLib,
             EntryPoint = "IGM_extract_isoline_from_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_extract_isoline_from_scalarWin(byte[] inBufferMesh,
                                                                int inSizeMesh,
                                                                byte[] inBufferScalar,
                                                                int inSizeScalar,
                                                                byte[] inBufferIsoValues,
                                                                int inSizeIsoValues,
                                                                out IntPtr outBuffer,
                                                                out int outSize);
  [DllImport(Platform.MacLib,
             EntryPoint = "IGM_extract_isoline_from_scalar",
             CallingConvention = CallingConvention.Cdecl)]
  private static extern bool IGM_extract_isoline_from_scalarMac(byte[] inBufferMesh,
                                                                int inSizeMesh,
                                                                byte[] inBufferScalar,
                                                                int inSizeScalar,
                                                                byte[] inBufferIsoValues,
                                                                int inSizeIsoValues,
                                                                out IntPtr outBuffer,
                                                                out int outSize);

  public static bool IGM_extract_isoline_from_scalar(byte[] inBufferMesh,
                                                     int inSizeMesh,
                                                     byte[] inBufferScalar,
                                                     int inSizeScalar,
                                                     byte[] inBufferIsoValues,
                                                     int inSizeIsoValues,
                                                     out IntPtr outBuffer,
                                                     out int outSize) {
    if (Platform.IsWindows)
      return IGM_extract_isoline_from_scalarWin(inBufferMesh,
                                                inSizeMesh,
                                                inBufferScalar,
                                                inSizeScalar,
                                                inBufferIsoValues,
                                                inSizeIsoValues,
                                                out outBuffer,
                                                out outSize);
    else
      return IGM_extract_isoline_from_scalarMac(inBufferMesh,
                                                inSizeMesh,
                                                inBufferScalar,
                                                inSizeScalar,
                                                inBufferIsoValues,
                                                inSizeIsoValues,
                                                out outBuffer,
                                                out outSize);
  }

#endregion
}
}

# GSP Extensions - How to Add Your Own C++ Functions

This folder contains example extension functions that demonstrate how to bridge
C# and C++ code using the GeoSharPlus library. Use these as templates for your
own custom functions.

## Architecture Overview

```
Your C# Code
     ?
     ?
???????????????????????????????????????????????????????????????????
?  GSP.Core.Serializer          - Converts types to byte[]       ?
?  GSP.Core.MarshalHelper       - Handles memory marshaling      ?
?  GSP.Core.Platform            - OS detection, library paths    ?
???????????????????????????????????????????????????????????????????
     ?
     ?
???????????????????????????????????????????????????????????????????
?  Your P/Invoke Bridge         - DllImport declarations         ?
?  Your High-Level Wrapper      - Clean API for users            ?
???????????????????????????????????????????????????????????????????
     ?
     ?
???????????????????????????????????????????????????????????????????
?  GeoSharPlusCPP.dll           - Your C++ implementation        ?
???????????????????????????????????????????????????????????????????
```

## Step-by-Step: Adding a New C++ Function

### Step 1: Write the C++ Function

In your C++ project, create a new function following this pattern:

```cpp
// In your .h file
GSP_API bool GSP_CALL my_custom_function(
    const uint8_t* inBuffer, int inSize,
    uint8_t** outBuffer, int* outSize);

// In your .cpp file
GSP_API bool GSP_CALL my_custom_function(
    const uint8_t* inBuffer, int inSize,
    uint8_t** outBuffer, int* outSize) {
    
    // 1. Initialize output
    *outBuffer = nullptr;
    *outSize = 0;
    
    // 2. Deserialize input
    std::vector<GeoSharPlusCPP::Vector3d> points;
    if (!GS::deserializePointArray(inBuffer, inSize, points)) {
        return false;
    }
    
    // 3. Do your processing
    // ... your algorithm here ...
    
    // 4. Serialize output
    if (!GS::serializePointArray(points, *outBuffer, *outSize)) {
        return false;
    }
    
    return true;
}
```

### Step 2: Add P/Invoke Declaration in C#

Create a bridge class with platform-specific imports:

```csharp
using System;
using System.Runtime.InteropServices;
using GSP.Core;

namespace MyProject {
    public static class MyBridge {
        // Windows
        [DllImport(Platform.WindowsLib, EntryPoint = "my_custom_function",
                   CallingConvention = CallingConvention.Cdecl)]
        private static extern bool MyCustomFunctionWin(
            byte[] inBuffer, int inSize,
            out IntPtr outBuffer, out int outSize);

        // macOS
        [DllImport(Platform.MacLib, EntryPoint = "my_custom_function",
                   CallingConvention = CallingConvention.Cdecl)]
        private static extern bool MyCustomFunctionMac(
            byte[] inBuffer, int inSize,
            out IntPtr outBuffer, out int outSize);

        // Cross-platform wrapper
        public static bool MyCustomFunction(
            byte[] inBuffer, int inSize,
            out IntPtr outBuffer, out int outSize) {
            
            if (Platform.IsWindows)
                return MyCustomFunctionWin(inBuffer, inSize, out outBuffer, out outSize);
            else
                return MyCustomFunctionMac(inBuffer, inSize, out outBuffer, out outSize);
        }
    }
}
```

### Step 3: Create High-Level Wrapper

Create a clean API that hides serialization details:

```csharp
using GSP.Core;
using GSP.Geometry;

namespace MyProject {
    public static class MyUtils {
        /// <summary>
        /// Processes points using my custom C++ algorithm.
        /// </summary>
        public static Vec3[]? ProcessPoints(Vec3[] points) {
            // 1. Serialize input
            var buffer = Serializer.Serialize(points);
            
            // 2. Call native function
            if (!MyBridge.MyCustomFunction(buffer, buffer.Length, 
                    out IntPtr outPtr, out int outSize))
                return null;
            
            // 3. Marshal result
            var resultBuffer = MarshalHelper.CopyAndFree(outPtr, outSize);
            
            // 4. Deserialize and return
            return Serializer.DeserializeVec3Array(resultBuffer);
        }
    }
}
```

### Step 4: Use with Rhino Types (Optional)

If working with Rhino, use the adapter:

```csharp
using GSP.Adapters.Rhino;
using Rhino.Geometry;

// Convert Rhino -> GSP -> Process -> GSP -> Rhino
var rhinoPoints = new Point3d[] { ... };
var gspPoints = rhinoPoints.ToGSP();
var result = MyUtils.ProcessPoints(gspPoints);
var rhinoResult = result?.ToRhino();
```

## Available Types

### GSP.Geometry (Platform-Independent)
- `Vec2` - 2D point/vector
- `Vec3` - 3D point/vector  
- `Mesh` - Mesh with vertices and faces

### GSP.Core.Serializer Methods
- `Serialize(Vec3)` / `DeserializeVec3(byte[])`
- `Serialize(Vec3[])` / `DeserializeVec3Array(byte[])`
- `Serialize(Mesh)` / `DeserializeMesh(byte[])`
- `Serialize(int[])` / `DeserializeIntArray(byte[])`
- `Serialize(double[])` / `DeserializeDoubleArray(byte[])`
- `Serialize((int,int)[])` / `DeserializeIntPairArray(byte[])`
- `Serialize((double,double)[])` / `DeserializeDoublePairArray(byte[])`

### GSP.Core.MarshalHelper Methods
- `CopyAndFree(IntPtr, int)` - Copy from unmanaged and free
- `Copy(IntPtr, int)` - Copy without freeing
- `Free(IntPtr)` - Free unmanaged memory

## File Organization

Recommended structure for your extensions:

```
YourProject/
??? Bridge/
?   ??? MyBridge.cs         # P/Invoke declarations
??? Utils/
?   ??? MyUtils.cs          # High-level wrappers
??? ...
```

## See Also

- `ExampleExtensions.cs` - Complete working examples
- C++ side: `GeoSharPlusCPP/include/GeoSharPlusCPP/Extensions/`

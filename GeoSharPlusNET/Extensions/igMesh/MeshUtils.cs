using Rhino.Geometry;
using System.Runtime.InteropServices;

using GSP;
using GSP.Adapters.Rhino;

namespace igMesh.Native {
public static class MeshUtils {
  private static Point3d Centroid(Mesh mesh) {
    // Serialize the mesh for calling into GeoSharPlusCPP
    byte[] meshBuffer;
    meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the C++ function to calculate centroid
    var success = igMeshBridge.MeshCentroid(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);
    if (!success) {
      throw new InvalidOperationException("Failed to calculate mesh centroid in native code.");
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    var outPt = Wrapper.FromPointBuffer(byteArray);

    return outPt;
  }

  public static (List<Point3d>, List<List<int>>, Point3d, double) GetMeshInfo(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Get vertices and faces directly from the mesh
    var V = new List<Point3d>(mesh.Vertices.ToPoint3dArray());
    List<List<int>> F = new List<List<int>>();
    foreach (var f in mesh.Faces) {
      // Check if quad mesh or not
      if (f.IsTriangle)
        F.Add(new List<int> { f.A, f.B, f.C });
      else
        F.Add(new List<int> { f.A, f.B, f.C, f.D });
    }

    // Calculate centroid using our C++ library
    Point3d cen = Centroid(mesh);

    // Use Rhino's native method for volume
    double vol = mesh.Volume();

    return (V, F, cen, vol);
  }

  public static bool LoadMesh(string fileName, out Mesh mesh) {
    mesh = new Mesh();
    if (string.IsNullOrEmpty(fileName)) {
      return false;
    }
    // Load the mesh using GeoSharPlusCPP
    var success = igMeshBridge.LoadMesh(fileName, out IntPtr outBuffer, out int outSize);
    if (!success || outBuffer == IntPtr.Zero) {
      return false;
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Convert the pointer to a Rhino Mesh object
    mesh = Wrapper.FromMeshBuffer(byteArray);
    return true;
  }

  public static bool SaveMesh(ref Mesh mesh, string fileName) {
    if (string.IsNullOrEmpty(fileName)) {
      return false;
    }

    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var success = igMeshBridge.SaveMesh(meshBuffer, meshBuffer.Length, fileName);

    return success;
  }

  /// <summary>
  /// Computes barycenter for each face of a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of vertex normals</returns>
  public static List<Point3d> GetBarycenter(ref Mesh rMesh) {
    // initialize the pointer and pass data
    if (rMesh == null)
      throw new ArgumentNullException(nameof(rMesh));

    var meshBuffer = Wrapper.ToMeshBuffer(rMesh);
    igMeshBridge.IGM_barycenter(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    var outPt = Wrapper.FromPointArrayBuffer(byteArray).ToList();

    return outPt;
  }

  /// <summary>
  /// Computes per-vertex normals for a mesh.
  /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of vertex normals</returns>
  public static List<Vector3d> GetNormalVert(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_vert_normals(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<Vector3d>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var normals = Wrapper.FromVector3dArrayBuffer(byteArray).ToList();

    return normals;
  }

  /// <summary>
  /// Computes per-face normals for a mesh.
  /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of face normals</returns>
  public static List<Vector3d> GetNormalFace(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_face_normals(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<Vector3d>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var normals = Wrapper.FromVector3dArrayBuffer(byteArray).ToList();

    return normals;
  }

  /// <summary>
  /// Computes per-corner normals for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="thresholdDegrees">Angle threshold in degrees for sharp
  /// features</param> <returns>List of lists of corner normals (one list per
  /// face)</returns>
  public static List<List<Vector3d>> GetNormalCorner(ref Mesh mesh,
                                                     double thresholdDegrees = 10.0) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_corner_normals(
        meshBuffer, meshBuffer.Length, thresholdDegrees, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<List<Vector3d>>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result - this is expected to be a 2D array
    var points = Wrapper.FromVector3dArrayBuffer(byteArray);

    // Organize into lists per face (assuming 3 corners per face for
    // triangular mesh)
    var cornerNormals = new List<List<Vector3d>>();
    for (int i = 0; i < points.Length; i += 3) {
      var faceCorners = new List<Vector3d>();
      for (int j = 0; j < 3 && (i + j) < points.Length; j++) {
        faceCorners.Add(points[i + j]);
      }
      cornerNormals.Add(faceCorners);
    }

    return cornerNormals;
  }

  /// <summary>
  /// Computes per-edge normals, edge indices, and edge map for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="weightingType">Type of weighting: 0=uniform, 1=area</param>
  /// <returns>Tuple containing edge normals, edge indices, and edge map</returns>
  public static (List<Vector3d> EdgeNormals, List<List<int>> EdgeIndices, List<int> EdgeMap)
      GetNormalEdge(ref Mesh mesh, int weightingType = 1) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function with multiple outputs
    var success = igMeshBridge.IGM_edge_normals(meshBuffer,
                                                meshBuffer.Length,
                                                weightingType,
                                                out IntPtr enBuffer,
                                                out int enSize,
                                                out IntPtr eiBuffer,
                                                out int eiSize,
                                                out IntPtr emapBuffer,
                                                out int emapSize);

    if (!success) {
      return (new List<Vector3d>(), new List<List<int>>(), new List<int>());
    }

    // Process edge normals
    var enBytes = new byte[enSize];
    Marshal.Copy(enBuffer, enBytes, 0, enSize);
    Marshal.FreeCoTaskMem(enBuffer);
    var edgeNormals = Wrapper.FromVector3dArrayBuffer(enBytes).ToList();

    // Process edge indices
    var eiBytes = new byte[eiSize];
    Marshal.Copy(eiBuffer, eiBytes, 0, eiSize);
    Marshal.FreeCoTaskMem(eiBuffer);
    var indices = Wrapper.FromIntPairArrayBufferToList(eiBytes);

    // Convert to the expected List<List<int>> format
    var edgeIndices = new List<List<int>>();
    foreach (var pair in indices) {
      edgeIndices.Add(new List<int> { pair.Item1, pair.Item2 });
    }

    // Process edge map
    var emapBytes = new byte[emapSize];
    Marshal.Copy(emapBuffer, emapBytes, 0, emapSize);
    Marshal.FreeCoTaskMem(emapBuffer);
    var edgeMap = Wrapper.FromIntArrayBufferToList(emapBytes);

    return (edgeNormals, edgeIndices, edgeMap);
  }

  /// <summary>
  /// Gets the vertex-vertex adjacency list for a mesh.
  /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of lists representing vertex-vertex adjacency</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<List<int>> GetAdjacencyVV(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function to get vertex-vertex adjacency
    var success = igMeshBridge.IGM_vert_vert_adjacency(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);
    if (!success || outBuffer == IntPtr.Zero) {
      return new List<List<int>>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result into a list of lists of integers
    var adjacencyList = Wrapper.FromNestedIntArrayBuffer(byteArray);
    return adjacencyList;
  }

  /// <summary>
  /// Gets the vertex-triangle adjacency for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>Tuple containing vertex-triangle adjacency and vertex-triangle
  /// indices</returns> <exception cref="ArgumentNullException"></exception>
  public static (List<List<int>> VT, List<List<int>> VTI) GetAdjacencyVT(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function to get vertex-triangle adjacency
    var success = igMeshBridge.IGM_vert_tri_adjacency(meshBuffer,
                                                      meshBuffer.Length,
                                                      out IntPtr outBufferVT,
                                                      out int outSizeVT,
                                                      out IntPtr outBufferVTI,
                                                      out int outSizeVTI);
    if (!success || outBufferVT == IntPtr.Zero || outBufferVTI == IntPtr.Zero) {
      return (new List<List<int>>(), new List<List<int>>());
    }

    // Copy the VT result from unmanaged memory to a managed byte array
    var byteArrayVT = new byte[outSizeVT];
    Marshal.Copy(outBufferVT, byteArrayVT, 0, outSizeVT);
    Marshal.FreeCoTaskMem(outBufferVT);  // Free the unmanaged memory

    // Copy the VTI result from unmanaged memory to a managed byte array
    var byteArrayVTI = new byte[outSizeVTI];
    Marshal.Copy(outBufferVTI, byteArrayVTI, 0, outSizeVTI);
    Marshal.FreeCoTaskMem(outBufferVTI);  // Free the unmanaged memory

    // Deserialize the results into lists of lists of integers
    var vtAdjacency = Wrapper.FromNestedIntArrayBuffer(byteArrayVT);
    var vtiAdjacency = Wrapper.FromNestedIntArrayBuffer(byteArrayVTI);

    return (vtAdjacency, vtiAdjacency);
  }

  /// <summary>
  /// Gets the triangle-triangle adjacency for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>Tuple containing triangle-triangle adjacency and
  /// triangle-triangle indices</returns> <exception
  /// cref="ArgumentNullException"></exception>
  public static (List<List<int>> TT, List<List<int>> TTI) GetAdjacencyTT(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function to get triangle-triangle adjacency
    var success = igMeshBridge.IGM_tri_tri_adjacency(meshBuffer,
                                                     meshBuffer.Length,
                                                     out IntPtr outBufferTT,
                                                     out int outSizeTT,
                                                     out IntPtr outBufferTTI,
                                                     out int outSizeTTI);
    if (!success || outBufferTT == IntPtr.Zero || outBufferTTI == IntPtr.Zero) {
      return (new List<List<int>>(), new List<List<int>>());
    }

    // Copy the TT result from unmanaged memory to a managed byte array
    var byteArrayTT = new byte[outSizeTT];
    Marshal.Copy(outBufferTT, byteArrayTT, 0, outSizeTT);
    Marshal.FreeCoTaskMem(outBufferTT);  // Free the unmanaged memory

    // Copy the TTI result from unmanaged memory to a managed byte array
    var byteArrayTTI = new byte[outSizeTTI];
    Marshal.Copy(outBufferTTI, byteArrayTTI, 0, outSizeTTI);
    Marshal.FreeCoTaskMem(outBufferTTI);  // Free the unmanaged memory

    // Deserialize the results into lists of lists of integers
    var ttAdjacency = Wrapper.FromNestedIntArrayBuffer(byteArrayTT);
    var ttiAdjacency = Wrapper.FromNestedIntArrayBuffer(byteArrayTTI);

    return (ttAdjacency, ttiAdjacency);
  }

  /// <summary>
  /// Gets boundary loops for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of lists representing boundary loops</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<List<int>> GetBoundaryLoop(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function to get boundary loops
    var success = igMeshBridge.IGM_boundary_loop(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);
    if (!success || outBuffer == IntPtr.Zero) {
      return new List<List<int>>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result into a list of lists of integers
    var boundaryLoops = Wrapper.FromNestedIntArrayBuffer(byteArray);
    return boundaryLoops;
  }

  /// <summary>
  /// Gets boundary edges for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>Tuple containing boundary edge geometry, edge indices,
  /// and triangle indices</returns> <exception
  /// cref="ArgumentNullException"></exception>
  public static (List<Line> EdgeGeometry, List<List<int>> EdgeIndices, List<int> TriangleIndices)
      GetBoundaryEdge(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function to get boundary facets
    var success = igMeshBridge.IGM_boundary_facet(meshBuffer,
                                                  meshBuffer.Length,
                                                  out IntPtr outBufferEL,
                                                  out int outSizeEL,
                                                  out IntPtr outBufferTL,
                                                  out int outSizeTL);
    if (!success || outBufferEL == IntPtr.Zero || outBufferTL == IntPtr.Zero) {
      return (new List<Line>(), new List<List<int>>(), new List<int>());
    }

    // Copy the EL result from unmanaged memory to a managed byte array
    var byteArrayEL = new byte[outSizeEL];
    Marshal.Copy(outBufferEL, byteArrayEL, 0, outSizeEL);
    Marshal.FreeCoTaskMem(outBufferEL);  // Free the unmanaged memory

    // Copy the TL result from unmanaged memory to a managed byte array
    var byteArrayTL = new byte[outSizeTL];
    Marshal.Copy(outBufferTL, byteArrayTL, 0, outSizeTL);
    Marshal.FreeCoTaskMem(outBufferTL);  // Free the unmanaged memory

    // Deserialize the results
    var edgeList = Wrapper.FromIntArrayBufferToList(byteArrayEL);
    var triangleIndices = Wrapper.FromIntArrayBufferToList(byteArrayTL);

    // Process edge list and create geometry
    List<Line> edgeGeometry = new List<Line>();
    List<List<int>> edgeIndices = new List<List<int>>();

    for (int i = 0; i < edgeList.Count; i += 2) {
      if (i + 1 < edgeList.Count) {
        int pt0 = edgeList[i];
        int pt1 = edgeList[i + 1];
        edgeIndices.Add(new List<int> { pt0, pt1 });
        edgeGeometry.Add(new Line(mesh.Vertices[pt0], mesh.Vertices[pt1]));
      }
    }

    return (edgeGeometry, edgeIndices, triangleIndices);
  }
  /// <summary>
  /// Remaps scalar values from vertices to faces by averaging.
  /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="vertexScalars">Scalar values defined on
  /// vertices</param> <returns>Scalar values remapped to
  /// faces</returns> <exception
  /// cref="ArgumentNullException"></exception>
  public static List<double> RemapVtoF(ref Mesh mesh, List<double> vertexScalars) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (vertexScalars == null)
      throw new ArgumentNullException(nameof(vertexScalars));
    if (vertexScalars.Count != mesh.Vertices.Count)
      throw new ArgumentException("Scalar count must match vertex count");

    // Serialize mesh and scalar data to buffers
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var scalarBuffer = Wrapper.ToDoubleArrayBuffer(vertexScalars);

    // Call the native function
    var success = igMeshBridge.IGM_remap_VtoF(meshBuffer,
                                              meshBuffer.Length,
                                              scalarBuffer,
                                              scalarBuffer.Length,
                                              out IntPtr outBuffer,
                                              out int outSize);
    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var faceScalars = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return faceScalars;
  }

  /// <summary>
  /// Remaps scalar values from faces to vertices by averaging.
  /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="faceScalars">Scalar values defined on faces</param>
  /// <returns>Scalar values remapped to vertices</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> RemapFtoV(ref Mesh mesh, List<double> faceScalars) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (faceScalars == null)
      throw new ArgumentNullException(nameof(faceScalars));
    if (faceScalars.Count != mesh.Faces.Count)
      throw new ArgumentException("Scalar count must match face count");

    // Serialize mesh and scalar data to buffers
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var scalarBuffer = Wrapper.ToDoubleArrayBuffer(faceScalars);

    // Call the native function
    var success = igMeshBridge.IGM_remap_FtoV(meshBuffer,
                                              meshBuffer.Length,
                                              scalarBuffer,
                                              scalarBuffer.Length,
                                              out IntPtr outBuffer,
                                              out int outSize);
    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var vertexScalars = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return vertexScalars;
  }

  /// <summary>
  /// Computes principal curvature directions and values for a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="radius">Radius parameter for curvature computation</param>
  /// <returns>Tuple containing principal directions and values</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static (List<Vector3d> PD1, List<Vector3d> PD2, List<double> PV1, List<double> PV2)
      GetPrincipalCurvature(ref Mesh mesh, uint radius = 5) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_principal_curvature(meshBuffer,
                                                       meshBuffer.Length,
                                                       radius,
                                                       out IntPtr obPD1,
                                                       out int obsPD1,
                                                       out IntPtr obPD2,
                                                       out int obsPD2,
                                                       out IntPtr obPV1,
                                                       out int obsPV1,
                                                       out IntPtr obPV2,
                                                       out int obsPV2);

    if (!success) {
      return (new List<Vector3d>(), new List<Vector3d>(), new List<double>(), new List<double>());
    }

    // Process PD1 (principal direction 1)
    var pd1Bytes = new byte[obsPD1];
    Marshal.Copy(obPD1, pd1Bytes, 0, obsPD1);
    Marshal.FreeCoTaskMem(obPD1);
    var pd1 = Wrapper.FromVector3dArrayBuffer(pd1Bytes).ToList();

    // Process PD2 (principal direction 2)
    var pd2Bytes = new byte[obsPD2];
    Marshal.Copy(obPD2, pd2Bytes, 0, obsPD2);
    Marshal.FreeCoTaskMem(obPD2);
    var pd2 = Wrapper.FromVector3dArrayBuffer(pd2Bytes).ToList();

    // Process PV1 (principal value 1)
    var pv1Bytes = new byte[obsPV1];
    Marshal.Copy(obPV1, pv1Bytes, 0, obsPV1);
    Marshal.FreeCoTaskMem(obPV1);
    var pv1 = Wrapper.FromDoubleArrayBufferToList(pv1Bytes);

    // Process PV2 (principal value 2)
    var pv2Bytes = new byte[obsPV2];
    Marshal.Copy(obPV2, pv2Bytes, 0, obsPV2);
    Marshal.FreeCoTaskMem(obPV2);
    var pv2 = Wrapper.FromDoubleArrayBufferToList(pv2Bytes);

    return (pd1, pd2, pv1, pv2);
  }

  /// <summary>
  /// Computes Gaussian curvature for each vertex of a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <returns>List of Gaussian curvature values</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetGaussianCurvature(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_gaussian_curvature(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var curvatures = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return curvatures;
  }

  /// <summary>
  /// Computes fast winding numbers for query points with respect to a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="queryPoints">Points to query</param>
  /// <returns>List of winding numbers</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetFastWindingNumber(ref Mesh mesh, ref List<Point3d> queryPoints) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (queryPoints == null)
      throw new ArgumentNullException(nameof(queryPoints));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Convert Point3d list to Vector3d list for serialization
    var queryVector3ds = queryPoints.ConvertAll(p => new Vector3d(p.X, p.Y, p.Z));
    var pointsBuffer = Wrapper.ToPointArrayBuffer(queryVector3ds);

    // Call the native function
    var success = igMeshBridge.IGM_fast_winding_number(meshBuffer,
                                                       meshBuffer.Length,
                                                       pointsBuffer,
                                                       pointsBuffer.Length,
                                                       out IntPtr outBuffer,
                                                       out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var windingNumbers = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return windingNumbers;
  }

  /// <summary>
  /// Computes signed distance from query points to a mesh.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="queryPoints">Points to query</param>
  /// <param name="signedType">Method for computing signed distance (1-4)</param>
  /// <returns>Tuple containing signed distances, face indices, and closest points</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static (List<double> SignedDistances, List<int> FaceIndices, List<Point3d> ClosestPoints)
      GetSignedDistance(ref Mesh mesh, ref List<Point3d> queryPoints, int signedType = 4) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (queryPoints == null)
      throw new ArgumentNullException(nameof(queryPoints));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Convert Point3d list to Vector3d list for serialization
    var queryVector3ds = queryPoints.ConvertAll(p => new Vector3d(p.X, p.Y, p.Z));
    var pointsBuffer = Wrapper.ToPointArrayBuffer(queryVector3ds);

    // Call the native function
    var success = igMeshBridge.IGM_signed_distance(meshBuffer,
                                                   meshBuffer.Length,
                                                   pointsBuffer,
                                                   pointsBuffer.Length,
                                                   signedType,
                                                   out IntPtr sdBuffer,
                                                   out int sdSize,
                                                   out IntPtr fiBuffer,
                                                   out int fiSize,
                                                   out IntPtr cpBuffer,
                                                   out int cpSize);

    if (!success) {
      return (new List<double>(), new List<int>(), new List<Point3d>());
    }

    // Process signed distances
    var sdBytes = new byte[sdSize];
    Marshal.Copy(sdBuffer, sdBytes, 0, sdSize);
    Marshal.FreeCoTaskMem(sdBuffer);
    var signedDistances = Wrapper.FromDoubleArrayBufferToList(sdBytes);

    // Process face indices
    var fiBytes = new byte[fiSize];
    Marshal.Copy(fiBuffer, fiBytes, 0, fiSize);
    Marshal.FreeCoTaskMem(fiBuffer);
    var faceIndices = Wrapper.FromIntArrayBufferToList(fiBytes);

    // Process closest points
    var cpBytes = new byte[cpSize];
    Marshal.Copy(cpBuffer, cpBytes, 0, cpSize);
    Marshal.FreeCoTaskMem(cpBuffer);
    var closestPoints = Wrapper.FromPointArrayBuffer(cpBytes).ToList();

    return (signedDistances, faceIndices, closestPoints);
  }

  /// <summary>
  /// Computes planarity values for quad faces in a mesh.
  /// /// </summary>
  /// <param name="mesh">Input quad mesh</param>
  /// <returns>List of planarity values</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetQuadPlanarity(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh preserving quads
    var meshBuffer = Wrapper.ToMeshBuffer(mesh, preserveQuads: true);

    // Call the native function
    var success = igMeshBridge.IGM_quad_planarity(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var planarityValues = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return planarityValues;
  }

  /// <summary>
  /// Planarizes quad faces in a mesh.
  /// /// </summary>
  /// <param name="mesh">Input quad mesh</param>
  /// <param name="maxIterations">Maximum iterations for planarization</param>
  /// <param name="threshold">Threshold to stop planarization</param>
  /// <returns>Planarized mesh</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static Mesh
  PlanarizeQuadMesh(ref Mesh mesh, int maxIterations = 100, double threshold = 0.005) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh preserving quads - critical for planarization to work
    var meshBuffer = Wrapper.ToMeshBuffer(mesh, preserveQuads: true);

    // Call the native function
    var success = igMeshBridge.IGM_planarize_quad_mesh(meshBuffer,
                                                       meshBuffer.Length,
                                                       maxIterations,
                                                       threshold,
                                                       out IntPtr outBuffer,
                                                       out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new Mesh();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var planarizedMesh = Wrapper.FromMeshBuffer(byteArray);
    return planarizedMesh;
  }

  /// <summary>
  /// Solves Laplacian equation with given boundary constraints.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="constraintIndices">Indices of constrained vertices</param>
  /// <param name="constraintValues">Values for constrained vertices</param>
  /// <returns>Scalar values for all vertices</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetLaplacianScalar(ref Mesh mesh,
                                                ref List<int> constraintIndices,
                                                ref List<double> constraintValues) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (constraintIndices == null)
      throw new ArgumentNullException(nameof(constraintIndices));
    if (constraintValues == null)
      throw new ArgumentNullException(nameof(constraintValues));
    if (constraintIndices.Count != constraintValues.Count)
      throw new ArgumentException("Constraint indices and values must have the same count");

    // Serialize mesh and constraint data to buffers
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var indicesBuffer = Wrapper.ToIntArrayBuffer(constraintIndices);
    var valuesBuffer = Wrapper.ToDoubleArrayBuffer(constraintValues);

    // Call the native function
    var success = igMeshBridge.IGM_laplacian_scalar(meshBuffer,
                                                    meshBuffer.Length,
                                                    indicesBuffer,
                                                    indicesBuffer.Length,
                                                    valuesBuffer,
                                                    valuesBuffer.Length,
                                                    out IntPtr outBuffer,
                                                    out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var scalarValues = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return scalarValues;
  }

  /// <summary>
  /// Computes harmonic parametrization of a mesh.
  /// Maps a mesh to a flat 2D domain using harmonic coordinates.
  /// The boundary vertices are mapped to a circle and the interior vertices
  /// are positioned to minimize the Dirichlet energy.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="k">Order of harmonic coordinates (typically 1 for biharmonic)</param>
  /// <returns>UV coordinates as 3D points (Z coordinate is 0)</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<Point3d> GetHarmonicParametrization(ref Mesh mesh, int k = 1) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_param_harmonic(
        meshBuffer, meshBuffer.Length, k, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<Point3d>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var uvCoordinates = Wrapper.FromPointArrayBuffer(byteArray).ToList();
    return uvCoordinates;
  }

  /// <summary>
  /// Precomputes data for heat-based geodesic distance calculations.
  /// This function computes and caches the necessary matrices for fast geodesic distance
  /// computation.
  /// /// </summary>
  /// <param name="mesh">Input mesh for geodesic computations</param>
  /// <returns>Handle for the precomputed data (to be used with GetHeatGeodesicDistances)</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static long GetHeatGeodesicPrecomputedData(ref Mesh mesh) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the native function
    var success = igMeshBridge.IGM_heat_geodesic_precompute(
        meshBuffer, meshBuffer.Length, out IntPtr outBuffer, out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return 0;  // Invalid handle
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the handle - assuming it's serialized as a double array for simplicity
    var handles = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return handles.Count > 0 ? (long)handles[0] : 0;
  }

  /// <summary>
  /// Computes heat-based geodesic distances from source vertices using precomputed data.
  /// This is the fast solving step that uses the precomputed factorization.
  /// /// </summary>
  /// <param name="precomputedHandle">Handle from GetHeatGeodesicPrecomputedData</param>
  /// <param name="sourceVertices">List of source vertex indices</param>
  /// <returns>Geodesic distances from sources to all vertices</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetHeatGeodesicDistances(long precomputedHandle,
                                                      ref List<int> sourceVertices) {
    if (sourceVertices == null)
      throw new ArgumentNullException(nameof(sourceVertices));

    // Serialize handle to buffer as double array (cast to double for serialization)
    var handleList = new List<double> { (double)precomputedHandle };
    var handleBuffer = Wrapper.ToDoubleArrayBuffer(handleList);

    // Serialize source vertices to buffer
    var sourcesBuffer = Wrapper.ToIntArrayBuffer(sourceVertices);

    // Call the native function
    var success = igMeshBridge.IGM_heat_geodesic_solve(handleBuffer,
                                                       handleBuffer.Length,
                                                       sourcesBuffer,
                                                       sourcesBuffer.Length,
                                                       out IntPtr outBuffer,
                                                       out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the distances
    var distances = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return distances;
  }

  /// <summary>
  /// Generates random or uniform distributed points on mesh surface.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="N">Number of points to generate</param>
  /// <param name="method">Sampling method: 0=random, 1=uniform</param>
  /// <returns>Tuple containing sampled points and their face indices</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static (List<Point3d> Points, List<int> FaceIndices)
      GetRandomPointsOnMesh(ref Mesh mesh, int N, int method = 0) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));

    // Serialize mesh to buffer
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);

    // Call the appropriate native function based on method
    bool success;
    IntPtr outBufferPoints, outBufferFI;
    int outSizePoints, outSizeFI;

    if (method == 0) {
      // Random sampling
      success = igMeshBridge.IGM_random_point_on_mesh(meshBuffer,
                                                      meshBuffer.Length,
                                                      N,
                                                      out outBufferPoints,
                                                      out outSizePoints,
                                                      out outBufferFI,
                                                      out outSizeFI);
    } else {
      // Blue noise (uniform) sampling
      success = igMeshBridge.IGM_blue_noise_sampling_on_mesh(meshBuffer,
                                                             meshBuffer.Length,
                                                             N,
                                                             out outBufferPoints,
                                                             out outSizePoints,
                                                             out outBufferFI,
                                                             out outSizeFI);
    }

    if (!success || outBufferPoints == IntPtr.Zero || outBufferFI == IntPtr.Zero) {
      return (new List<Point3d>(), new List<int>());
    }

    // Copy points from unmanaged memory
    var pointsBytes = new byte[outSizePoints];
    Marshal.Copy(outBufferPoints, pointsBytes, 0, outSizePoints);
    Marshal.FreeCoTaskMem(outBufferPoints);

    // Copy face indices from unmanaged memory
    var fiBytes = new byte[outSizeFI];
    Marshal.Copy(outBufferFI, fiBytes, 0, outSizeFI);
    Marshal.FreeCoTaskMem(outBufferFI);

    // Deserialize the results
    var points = Wrapper.FromPointArrayBuffer(pointsBytes).ToList();
    var faceIndices = Wrapper.FromIntArrayBufferToList(fiBytes);

    return (points, faceIndices);
  }

  /// <summary>
  /// Computes a constrained scalar field on mesh vertices using Laplacian smoothing.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="constraintIndices">Indices of constrained vertices</param>
  /// <param name="constraintValues">Values for constrained vertices</param>
  /// <returns>Scalar values for all vertices</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<double> GetConstrainedScalar(ref Mesh mesh,
                                                  ref List<int> constraintIndices,
                                                  ref List<double> constraintValues) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (constraintIndices == null)
      throw new ArgumentNullException(nameof(constraintIndices));
    if (constraintValues == null)
      throw new ArgumentNullException(nameof(constraintValues));
    if (constraintIndices.Count != constraintValues.Count)
      throw new ArgumentException("Constraint indices and values must have the same count");

    // Serialize mesh and constraint data to buffers
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var indicesBuffer = Wrapper.ToIntArrayBuffer(constraintIndices);
    var valuesBuffer = Wrapper.ToDoubleArrayBuffer(constraintValues);

    // Call the native function
    var success = igMeshBridge.IGM_constrained_scalar(meshBuffer,
                                                      meshBuffer.Length,
                                                      indicesBuffer,
                                                      indicesBuffer.Length,
                                                      valuesBuffer,
                                                      valuesBuffer.Length,
                                                      out IntPtr outBuffer,
                                                      out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<double>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize the result
    var scalarValues = Wrapper.FromDoubleArrayBufferToList(byteArray);
    return scalarValues;
  }

  /// <summary>
  /// Extracts isoline points from a scalar field defined on mesh vertices.
  /// Points are sorted into connected polylines for each isoline value.
  /// /// </summary>
  /// <param name="mesh">Input mesh</param>
  /// <param name="meshScalar">Scalar values defined on vertices</param>
  /// <param name="isoValues">Isoline parameter values (typically in [0, 1])</param>
  /// <returns>List of lists of points, one sorted polyline for each isoline</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static List<List<Point3d>>
  GetIsolineFromScalar(ref Mesh mesh, ref List<double> meshScalar, ref List<double> isoValues) {
    if (mesh == null)
      throw new ArgumentNullException(nameof(mesh));
    if (meshScalar == null)
      throw new ArgumentNullException(nameof(meshScalar));
    if (isoValues == null)
      throw new ArgumentNullException(nameof(isoValues));

    // Serialize mesh and scalar data to buffers
    var meshBuffer = Wrapper.ToMeshBuffer(mesh);
    var scalarBuffer = Wrapper.ToDoubleArrayBuffer(meshScalar);
    var isoBuffer = Wrapper.ToDoubleArrayBuffer(isoValues);

    // Call the native function
    var success = igMeshBridge.IGM_extract_isoline_from_scalar(meshBuffer,
                                                               meshBuffer.Length,
                                                               scalarBuffer,
                                                               scalarBuffer.Length,
                                                               isoBuffer,
                                                               isoBuffer.Length,
                                                               out IntPtr outBuffer,
                                                               out int outSize);

    if (!success || outBuffer == IntPtr.Zero) {
      return new List<List<Point3d>>();
    }

    // Copy the result from unmanaged memory to a managed byte array
    var byteArray = new byte[outSize];
    Marshal.Copy(outBuffer, byteArray, 0, outSize);
    Marshal.FreeCoTaskMem(outBuffer);  // Free the unmanaged memory

    // Deserialize all points
    var allPoints = Wrapper.FromPointArrayBuffer(byteArray);

    if (allPoints.Length == 0) {
      return new List<List<Point3d>>();
    }

    // Group points by isoline and sort them into connected polylines
    int pointsPerIsoline = allPoints.Length / isoValues.Count;
    var isolinePolylines = new List<List<Point3d>>();

    for (int isoIdx = 0; isoIdx < isoValues.Count; isoIdx++) {
      int startIdx = isoIdx * pointsPerIsoline;
      int endIdx = Math.Min(startIdx + pointsPerIsoline, allPoints.Length);

      if (startIdx >= endIdx) {
        isolinePolylines.Add(new List<Point3d>());
        continue;
      }

      // Extract points for this isoline
      var isolinePoints = new List<Point3d>();
      for (int j = startIdx; j < endIdx; j++) {
        isolinePoints.Add(allPoints[j]);
      }

      // Sort points into a connected polyline using greedy nearest-neighbor
      var sortedPolyline = SortPointsIntoPolyline(isolinePoints);
      isolinePolylines.Add(sortedPolyline);
    }

    return isolinePolylines;
  }

  /// <summary>
  /// Helper method to sort unordered points into a connected polyline.
  /// Uses KD-tree for fast neighbor queries and direction clustering for endpoint detection.
  /// Removes duplicate/overlapping points before sorting.
  /// </summary>
  private static List<Point3d> SortPointsIntoPolyline(List<Point3d> points) {
    if (points.Count == 0)
      return new List<Point3d>();
    if (points.Count == 1)
      return new List<Point3d>(points);

    // Phase 1: Remove duplicate points
    const double tolerance = 1e-6;
    var uniquePoints = new List<Point3d>();

    foreach (var point in points) {
      bool isDuplicate = false;
      foreach (var existing in uniquePoints) {
        if (point.DistanceTo(existing) < tolerance) {
          isDuplicate = true;
          break;
        }
      }
      if (!isDuplicate) {
        uniquePoints.Add(point);
      }
    }

    if (uniquePoints.Count == 0)
      return new List<Point3d>();
    if (uniquePoints.Count == 1)
      return new List<Point3d>(uniquePoints);

    // Phase 2: Build KD-tree for fast neighbor queries
    var kdTree = new KdTree.KdTree<double, int>(3, new KdTree.Math.DoubleMath());
    for (int i = 0; i < uniquePoints.Count; i++) {
      var pt = uniquePoints[i];
      kdTree.Add(new double[] { pt.X, pt.Y, pt.Z }, i);
    }

    // Phase 3: Find endpoint using direction clustering
    int startIdx = FindPolylineEndpointWithClustering(uniquePoints, kdTree);

    // Phase 4: Greedy nearest-neighbor sorting using KD-tree
    var sorted = new List<Point3d>();
    var remaining = new HashSet<int>(Enumerable.Range(0, uniquePoints.Count));

    sorted.Add(uniquePoints[startIdx]);
    remaining.Remove(startIdx);

    while (remaining.Count > 0) {
      Point3d current = sorted[sorted.Count - 1];

      // Query KD-tree for K nearest neighbors
      var nearest = kdTree.GetNearestNeighbours(new double[] { current.X, current.Y, current.Z },
                                                Math.Min(10, uniquePoints.Count));

      // Find closest unvisited neighbor
      int nearestIdx = -1;
      double minDist = double.MaxValue;

      foreach (var neighbor in nearest) {
        int idx = neighbor.Value;
        if (remaining.Contains(idx)) {
          double dist = current.DistanceTo(uniquePoints[idx]);
          if (dist < minDist) {
            minDist = dist;
            nearestIdx = idx;
          }
        }
      }

      if (nearestIdx >= 0) {
        sorted.Add(uniquePoints[nearestIdx]);
        remaining.Remove(nearestIdx);
      } else {
        break;
      }
    }

    return sorted;
  }

  /// <summary>
  /// Finds an endpoint using direction clustering.
  /// Points where all neighbors point in the same direction are endpoints.
  /// </summary>
  private static int FindPolylineEndpointWithClustering(List<Point3d> points,
                                                        KdTree.KdTree<double, int> kdTree) {
    if (points.Count <= 2)
      return 0;

    int bestEndpointIdx = 0;
    double bestScore = double.MaxValue;
    int K = Math.Min(8, points.Count - 1);  // Changed from const to var

    for (int i = 0; i < points.Count; i++) {
      Point3d current = points[i];

      // Get K nearest neighbors
      var nearest = kdTree.GetNearestNeighbours(new double[] { current.X, current.Y, current.Z },
                                                K + 1  // +1 because it includes the point itself
      );

      // Extract neighbor directions (skip self)
      var directions = new List<Vector3d>();
      var distances = new List<double>();

      foreach (var neighbor in nearest) {
        int idx = neighbor.Value;
        if (idx == i)
          continue;

        Point3d neighborPt = points[idx];
        Vector3d dir = neighborPt - current;
        double dist = dir.Length;

        if (dist > 1e-10) {
          dir.Unitize();
          directions.Add(dir);
          distances.Add(dist);
        }
      }

      if (directions.Count == 0) {
        continue;  // Skip isolated points
      }

      if (directions.Count == 1) {
        // Only 1 neighbor - perfect endpoint
        return i;
      }

      // Cluster directions by angular similarity
      var clusters = ClusterDirections(directions, angleThresholdDeg: 45.0);

      double score;

      if (clusters.Count == 1) {
        // All neighbors point same direction - ENDPOINT!
        score = 0.0;
      } else if (clusters.Count == 2) {
        // Two clusters - check if opposite
        Vector3d centroid1 = CalculateDirectionCentroid(clusters[0]);
        Vector3d centroid2 = CalculateDirectionCentroid(clusters[1]);

        double dotProduct = centroid1 * centroid2;
        dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));
        double angle = Math.Acos(dotProduct) * 180.0 / Math.PI;

        if (angle >= 160.0 && angle <= 180.0) {
          // Opposite directions - middle point
          score = 180.0;
        } else {
          // Non-opposite - corner or endpoint
          score = 90.0;
        }
      } else {
        // 3+ clusters - irregular geometry, avoid as start
        score = 200.0;
      }

      // Additional check: distance variance
      if (distances.Count >= 2) {
        double meanDist = distances.Average();
        double variance = distances.Select(d => Math.Pow(d - meanDist, 2)).Average();
        double stdDev = Math.Sqrt(variance);
        double varianceScore = stdDev / (meanDist + 1e-10);

        if (varianceScore > 1.5) {
          // High variance = unbalanced = endpoint indicator
          score = Math.Min(score, 30.0);
        }
      }

      if (score < bestScore) {
        bestScore = score;
        bestEndpointIdx = i;
      }

      // Early exit for perfect endpoint
      if (score == 0.0) {
        return bestEndpointIdx;
      }
    }

    return bestEndpointIdx;
  }

  /// <summary>
  /// Clusters direction vectors by angular similarity.
  /// </summary>
  private static List<List<Vector3d>> ClusterDirections(List<Vector3d> directions,
                                                        double angleThresholdDeg) {
    var clusters = new List<List<Vector3d>>();
    double angleThresholdRad = angleThresholdDeg * Math.PI / 180.0;

    foreach (var dir in directions) {
      bool addedToCluster = false;

      foreach (var cluster in clusters) {
        Vector3d centroid = CalculateDirectionCentroid(cluster);
        double dotProduct = dir * centroid;
        dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));
        double angle = Math.Acos(dotProduct);

        if (angle < angleThresholdRad) {
          cluster.Add(dir);
          addedToCluster = true;
          break;
        }
      }

      if (!addedToCluster) {
        clusters.Add(new List<Vector3d> { dir });
      }
    }

    return clusters;
  }

  /// <summary>
  /// Calculates the centroid (average direction) of a cluster of direction vectors.
  /// </summary>
  private static Vector3d CalculateDirectionCentroid(List<Vector3d> directions) {
    if (directions.Count == 0)
      return new Vector3d(1, 0, 0);

    Vector3d sum = new Vector3d(0, 0, 0);
    foreach (var dir in directions) {
      sum += dir;
    }

    double length = sum.Length;
    if (length > 1e-10) {
      sum.Unitize();
    } else {
      sum = new Vector3d(1, 0, 0);
    }

    return sum;
  }
}
}  // namespace igMesh.Native

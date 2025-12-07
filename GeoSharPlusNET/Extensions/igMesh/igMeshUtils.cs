using System;
using System.Runtime.InteropServices;
using GSP;
using GSP.Adapters.Rhino;
using GSP.Core;
using Rhino.Geometry;

namespace igMesh.Native {
/// <summary>
/// High-level utilities for igMesh operations.
/// These methods provide a clean API that handles serialization and marshaling internally.
/// </summary>
public static class igMeshUtils {
#region Mesh I / O

  /// <summary>
  /// Loads a mesh from a file (OBJ, PLY, etc.)
  /// </summary>
  public static Mesh? LoadMesh(string filePath) {
    if (!igMeshBridge.LoadMesh(filePath, out IntPtr outPtr, out int outSize))
      return null;
    byte[] buffer = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Wrapper.FromMeshBuffer(buffer);
  }

  /// <summary>
  /// Saves a mesh to a file.
  /// </summary>
  public static bool SaveMesh(Mesh mesh, string filePath) {
    byte[] buffer = Wrapper.ToMeshBuffer(mesh);
    return igMeshBridge.SaveMesh(buffer, buffer.Length, filePath);
  }

#endregion

#region Basic Mesh Operations

  /// <summary>
  /// Computes the centroid of a mesh.
  /// </summary>
  public static Point3d MeshCentroid(Mesh mesh) {
    byte[] buffer = Wrapper.ToMeshBuffer(mesh);
    if (!igMeshBridge.MeshCentroid(buffer, buffer.Length, out IntPtr outPtr, out int outSize))
      return Point3d.Unset;
    byte[] result = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Wrapper.FromPointBuffer(result);
  }

  /// <summary>
  /// Computes the barycenter of each face.
  /// </summary>
  public static Point3d[]? Barycenter(Mesh mesh) {
    byte[] buffer = Wrapper.ToMeshBuffer(mesh);
    if (!igMeshBridge.IGM_barycenter(buffer, buffer.Length, out IntPtr outPtr, out int outSize))
      return null;
    byte[] result = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Wrapper.FromPointArrayBuffer(result);
  }

  /// <summary>
  /// Computes per-vertex normals.
  /// </summary>
  public static Vector3d[]? VertexNormals(Mesh mesh) {
    byte[] buffer = Wrapper.ToMeshBuffer(mesh);
    if (!igMeshBridge.IGM_vert_normals(buffer, buffer.Length, out IntPtr outPtr, out int outSize))
      return null;
    byte[] result = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Wrapper.FromVector3dArrayBuffer(result);
  }

#endregion

#region Scalar Field Operations

  /// <summary>
  /// Computes Laplacian scalar field on a mesh with boundary constraints.
  /// </summary>
  public static double[]? LaplacianScalar(Mesh mesh,
                                          int[] constraintIndices,
                                          double[] constraintValues) {
    byte[] meshBuffer = Wrapper.ToMeshBuffer(mesh);
    byte[] indicesBuffer = Serializer.Serialize(constraintIndices);
    byte[] valuesBuffer = Serializer.Serialize(constraintValues);

    if (!igMeshBridge.IGM_laplacian_scalar(meshBuffer,
                                           meshBuffer.Length,
                                           indicesBuffer,
                                           indicesBuffer.Length,
                                           valuesBuffer,
                                           valuesBuffer.Length,
                                           out IntPtr outPtr,
                                           out int outSize))
      return null;

    byte[] resultBuffer = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Serializer.DeserializeDoubleArray(resultBuffer);
  }

  /// <summary>
  /// Computes constrained scalar field on a mesh.
  /// </summary>
  public static double[]? ConstrainedScalar(Mesh mesh,
                                            int[] constraintIndices,
                                            double[] constraintValues) {
    byte[] meshBuffer = Wrapper.ToMeshBuffer(mesh);
    byte[] indicesBuffer = Serializer.Serialize(constraintIndices);
    byte[] valuesBuffer = Serializer.Serialize(constraintValues);

    if (!igMeshBridge.IGM_constrained_scalar(meshBuffer,
                                             meshBuffer.Length,
                                             indicesBuffer,
                                             indicesBuffer.Length,
                                             valuesBuffer,
                                             valuesBuffer.Length,
                                             out IntPtr outPtr,
                                             out int outSize))
      return null;

    byte[] resultBuffer = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Serializer.DeserializeDoubleArray(resultBuffer);
  }

#endregion

#region Isoline Extraction

  /// <summary>
  /// Extracts isolines from a scalar field at specified iso-values.
  /// </summary>
  public static Point3d[]? ExtractIsolines(Mesh mesh, double[] scalarField, double[] isoValues) {
    byte[] meshBuffer = Wrapper.ToMeshBuffer(mesh);
    byte[] scalarBuffer = Serializer.Serialize(scalarField);
    byte[] isoBuffer = Serializer.Serialize(isoValues);

    if (!igMeshBridge.IGM_extract_isoline_from_scalar(meshBuffer,
                                                      meshBuffer.Length,
                                                      scalarBuffer,
                                                      scalarBuffer.Length,
                                                      isoBuffer,
                                                      isoBuffer.Length,
                                                      out IntPtr outPtr,
                                                      out int outSize))
      return null;

    byte[] resultBuffer = MarshalHelper.CopyAndFree(outPtr, outSize);
    return Wrapper.FromPointArrayBuffer(resultBuffer);
  }

#endregion
}
}

// ============================================
// Rhino Geometry Adapter
// ============================================
// This file provides conversions between Rhino.Geometry types
// and GSP.Geometry types for use with the GeoSharPlus library.
//
// To use this file, your project must reference RhinoCommon.
// ============================================

using System;
using System.Linq;
using Rhino.Geometry;

namespace GSP.Adapters.Rhino {
  /// <summary>
  /// Adapter for converting between Rhino.Geometry types and GSP.Geometry types.
  /// </summary>
  public class RhinoAdapter : GSP.Geometry.IGeometryAdapter<Point3d, global::Rhino.Geometry.Mesh> {
    /// <summary>
    /// Singleton instance of the adapter.
    /// </summary>
    public static RhinoAdapter Instance { get; } = new();

    #region Point Conversions

    /// <summary>
    /// Converts a Rhino Point3d to a GSP Vec3.
    /// </summary>
    public GSP.Geometry.Vec3 PointToGSP(Point3d point) =>
        new(point.X, point.Y, point.Z);

    /// <summary>
    /// Converts a GSP Vec3 to a Rhino Point3d.
    /// </summary>
    public Point3d PointFromGSP(GSP.Geometry.Vec3 point) =>
        new(point.X, point.Y, point.Z);

    /// <summary>
    /// Converts an array of Rhino Point3d to GSP Vec3 array.
    /// </summary>
    public GSP.Geometry.Vec3[] PointsToGSP(Point3d[] points) =>
        points.Select(p => new GSP.Geometry.Vec3(p.X, p.Y, p.Z)).ToArray();

    /// <summary>
    /// Converts a GSP Vec3 array to Rhino Point3d array.
    /// </summary>
    public Point3d[] PointsFromGSP(GSP.Geometry.Vec3[] points) =>
        points.Select(p => new Point3d(p.X, p.Y, p.Z)).ToArray();

    #endregion

    #region Vector Conversions

    /// <summary>
    /// Converts a Rhino Vector3d to a GSP Vec3.
    /// </summary>
    public GSP.Geometry.Vec3 VectorToGSP(Vector3d vector) =>
        new(vector.X, vector.Y, vector.Z);

    /// <summary>
    /// Converts a GSP Vec3 to a Rhino Vector3d.
    /// </summary>
    public Vector3d VectorFromGSP(GSP.Geometry.Vec3 vector) =>
        new(vector.X, vector.Y, vector.Z);

    #endregion

    #region Mesh Conversions

    /// <summary>
    /// Converts a Rhino Mesh to a GSP Mesh.
    /// </summary>
    public GSP.Geometry.Mesh MeshToGSP(global::Rhino.Geometry.Mesh mesh) {
      var result = new GSP.Geometry.Mesh();

      // Convert vertices
      result.Vertices = mesh.Vertices
          .Select(v => new GSP.Geometry.Vec3(v.X, v.Y, v.Z))
          .ToArray();

      // Separate triangles and quads
      var triangles = new System.Collections.Generic.List<(int, int, int)>();
      var quads = new System.Collections.Generic.List<(int, int, int, int)>();

      foreach (var face in mesh.Faces) {
        if (face.IsTriangle) {
          triangles.Add((face.A, face.B, face.C));
        } else {
          quads.Add((face.A, face.B, face.C, face.D));
        }
      }

      result.TriangleFaces = triangles.ToArray();
      result.QuadFaces = quads.ToArray();

      return result;
    }

    /// <summary>
    /// Converts a GSP Mesh to a Rhino Mesh.
    /// </summary>
    public global::Rhino.Geometry.Mesh MeshFromGSP(GSP.Geometry.Mesh gspMesh) {
      var mesh = new global::Rhino.Geometry.Mesh();

      // Add vertices
      foreach (var v in gspMesh.Vertices) {
        mesh.Vertices.Add(v.X, v.Y, v.Z);
      }

      // Add triangle faces
      foreach (var (a, b, c) in gspMesh.TriangleFaces) {
        mesh.Faces.AddFace(a, b, c);
      }

      // Add quad faces
      foreach (var (a, b, c, d) in gspMesh.QuadFaces) {
        mesh.Faces.AddFace(a, b, c, d);
      }

      // Clean up mesh
      if (mesh.IsValid) {
        mesh.RebuildNormals();
        mesh.Compact();
      }

      return mesh;
    }

    #endregion
  }

  /// <summary>
  /// Extension methods for converting between Rhino and GSP types.
  /// </summary>
  public static class RhinoExtensions {
    private static readonly RhinoAdapter _adapter = RhinoAdapter.Instance;

    // Point3d extensions
    public static GSP.Geometry.Vec3 ToGSP(this Point3d point) => _adapter.PointToGSP(point);
    public static Point3d ToRhino(this GSP.Geometry.Vec3 point) => _adapter.PointFromGSP(point);

    // Point3d array extensions
    public static GSP.Geometry.Vec3[] ToGSP(this Point3d[] points) => _adapter.PointsToGSP(points);
    public static Point3d[] ToRhino(this GSP.Geometry.Vec3[] points) => _adapter.PointsFromGSP(points);

    // Vector3d extensions
    public static GSP.Geometry.Vec3 ToGSP(this Vector3d vector) => _adapter.VectorToGSP(vector);
    public static Vector3d ToRhinoVector(this GSP.Geometry.Vec3 vector) => _adapter.VectorFromGSP(vector);

    // Mesh extensions
    public static GSP.Geometry.Mesh ToGSP(this global::Rhino.Geometry.Mesh mesh) => _adapter.MeshToGSP(mesh);
    public static global::Rhino.Geometry.Mesh ToRhino(this GSP.Geometry.Mesh mesh) => _adapter.MeshFromGSP(mesh);
  }
}

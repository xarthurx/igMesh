using System;
using System.Collections.Generic;
using System.Linq;

namespace GSP.Geometry {
  /// <summary>
  /// A simple mesh structure containing vertices and faces.
  /// Platform-independent - no CAD software dependencies.
  /// </summary>
  /// <remarks>
  /// Supports both triangle and quad faces.
  /// Use TriangleFaces for triangle meshes, QuadFaces for quad meshes.
  /// </remarks>
  public class Mesh {
    /// <summary>
    /// The mesh vertices as an array of Vec3.
    /// </summary>
    public Vec3[] Vertices { get; set; } = Array.Empty<Vec3>();

    /// <summary>
    /// Triangle faces as tuples of vertex indices (A, B, C).
    /// </summary>
    public (int A, int B, int C)[] TriangleFaces { get; set; } = Array.Empty<(int, int, int)>();

    /// <summary>
    /// Quad faces as tuples of vertex indices (A, B, C, D).
    /// </summary>
    public (int A, int B, int C, int D)[] QuadFaces { get; set; } = Array.Empty<(int, int, int, int)>();

    /// <summary>
    /// Creates an empty mesh.
    /// </summary>
    public Mesh() { }

    /// <summary>
    /// Creates a triangle mesh from vertices and faces.
    /// </summary>
    public Mesh(Vec3[] vertices, (int A, int B, int C)[] faces) {
      Vertices = vertices;
      TriangleFaces = faces;
    }

    /// <summary>
    /// Creates a quad mesh from vertices and faces.
    /// </summary>
    public Mesh(Vec3[] vertices, (int A, int B, int C, int D)[] faces) {
      Vertices = vertices;
      QuadFaces = faces;
    }

    /// <summary>
    /// Returns true if the mesh has triangle faces.
    /// </summary>
    public bool HasTriangles => TriangleFaces.Length > 0;

    /// <summary>
    /// Returns true if the mesh has quad faces.
    /// </summary>
    public bool HasQuads => QuadFaces.Length > 0;

    /// <summary>
    /// Returns the total number of faces (triangles + quads).
    /// </summary>
    public int FaceCount => TriangleFaces.Length + QuadFaces.Length;

    /// <summary>
    /// Returns the number of vertices.
    /// </summary>
    public int VertexCount => Vertices.Length;

    /// <summary>
    /// Returns true if the mesh has valid data.
    /// </summary>
    public bool IsValid => Vertices.Length >= 3 && FaceCount > 0;

    /// <summary>
    /// Converts all quad faces to triangles.
    /// Each quad (A, B, C, D) becomes two triangles: (A, B, C) and (A, C, D).
    /// </summary>
    public void Triangulate() {
      if (!HasQuads) return;

      var newTriangles = new List<(int, int, int)>(TriangleFaces);
      foreach (var (A, B, C, D) in QuadFaces) {
        newTriangles.Add((A, B, C));
        newTriangles.Add((A, C, D));
      }

      TriangleFaces = newTriangles.ToArray();
      QuadFaces = Array.Empty<(int, int, int, int)>();
    }

    /// <summary>
    /// Creates a deep copy of this mesh.
    /// </summary>
    public Mesh Clone() {
      return new Mesh {
        Vertices = Vertices.ToArray(),
        TriangleFaces = TriangleFaces.ToArray(),
        QuadFaces = QuadFaces.ToArray()
      };
    }

    /// <summary>
    /// Computes the bounding box of the mesh.
    /// </summary>
    /// <returns>Tuple of (min, max) corners.</returns>
    public (Vec3 Min, Vec3 Max) GetBoundingBox() {
      if (Vertices.Length == 0)
        return (Vec3.Zero, Vec3.Zero);

      double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
      double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

      foreach (var v in Vertices) {
        if (v.X < minX) minX = v.X;
        if (v.Y < minY) minY = v.Y;
        if (v.Z < minZ) minZ = v.Z;
        if (v.X > maxX) maxX = v.X;
        if (v.Y > maxY) maxY = v.Y;
        if (v.Z > maxZ) maxZ = v.Z;
      }

      return (new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ));
    }

    public override string ToString() =>
        $"Mesh [V:{VertexCount}, T:{TriangleFaces.Length}, Q:{QuadFaces.Length}]";
  }
}

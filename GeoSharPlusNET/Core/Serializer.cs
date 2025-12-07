using System;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;

namespace GSP.Core {
  /// <summary>
  /// Serialization utilities for converting geometry types to/from FlatBuffer byte arrays.
  /// Use these methods to prepare data for C++ calls and parse results.
  /// </summary>
  /// <remarks>
  /// This class provides serialization for GSP.Geometry types (Vec3, Mesh, etc.)
  /// which are platform-independent. For CAD-specific types (Rhino, AutoCAD, etc.),
  /// use the appropriate adapter or the Wrapper class with Rhino types.
  /// </remarks>
  public static class Serializer {
    #region Vec3 Serialization

    /// <summary>
    /// Serializes a Vec3 point to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(Geometry.Vec3 point) {
      var builder = new FlatBufferBuilder(64);

      FB.PointData.StartPointData(builder);
      var vecOffset = FB.Vec3.CreateVec3(builder, point.X, point.Y, point.Z);
      FB.PointData.AddPoint(builder, vecOffset);
      var ptOffset = FB.PointData.EndPointData(builder);

      builder.Finish(ptOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to a Vec3 point.
    /// </summary>
    public static Geometry.Vec3 DeserializeVec3(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var ptData = FB.PointData.GetRootAsPointData(byteBuffer);
      var pt = ptData.Point;

      return pt.HasValue
          ? new Geometry.Vec3(pt.Value.X, pt.Value.Y, pt.Value.Z)
          : Geometry.Vec3.Zero;
    }

    #endregion

    #region Vec3 Array Serialization

    /// <summary>
    /// Serializes an array of Vec3 points to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(Geometry.Vec3[] points) {
      var builder = new FlatBufferBuilder(1024);

      FB.PointArrayData.StartPointsVector(builder, points.Length);
      for (int i = points.Length - 1; i >= 0; i--) {
        FB.Vec3.CreateVec3(builder, points[i].X, points[i].Y, points[i].Z);
      }
      var ptOffset = builder.EndVector();

      var arrayOffset = FB.PointArrayData.CreatePointArrayData(builder, ptOffset);
      builder.Finish(arrayOffset.Value);

      return builder.SizedByteArray();
    }

    /// <summary>
    /// Serializes a list of Vec3 points to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(List<Geometry.Vec3> points) => Serialize(points.ToArray());

    /// <summary>
    /// Deserializes a FlatBuffer byte array to an array of Vec3 points.
    /// </summary>
    public static Geometry.Vec3[] DeserializeVec3Array(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var pointArray = FB.PointArrayData.GetRootAsPointArrayData(byteBuffer);

      if (pointArray.PointsLength == 0)
        return Array.Empty<Geometry.Vec3>();

      var result = new Geometry.Vec3[pointArray.PointsLength];
      for (int i = 0; i < pointArray.PointsLength; i++) {
        var pt = pointArray.Points(i);
        result[i] = pt.HasValue
            ? new Geometry.Vec3(pt.Value.X, pt.Value.Y, pt.Value.Z)
            : Geometry.Vec3.Zero;
      }
      return result;
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to a list of Vec3 points.
    /// </summary>
    public static List<Geometry.Vec3> DeserializeVec3List(byte[] buffer) =>
        new List<Geometry.Vec3>(DeserializeVec3Array(buffer));

    #endregion

    #region Mesh Serialization

    /// <summary>
    /// Serializes a Mesh to a FlatBuffer byte array.
    /// </summary>
    /// <param name="mesh">The mesh to serialize.</param>
    /// <param name="triangulate">If true, converts quads to triangles before serializing.</param>
    public static byte[] Serialize(Geometry.Mesh mesh, bool triangulate = false) {
      var builder = new FlatBufferBuilder(1024);

      // Optionally triangulate
      var workingMesh = mesh;
      if (triangulate && mesh.HasQuads) {
        workingMesh = mesh.Clone();
        workingMesh.Triangulate();
      }

      // Add vertices
      FB.MeshData.StartVerticesVector(builder, workingMesh.Vertices.Length);
      for (int i = workingMesh.Vertices.Length - 1; i >= 0; i--) {
        var v = workingMesh.Vertices[i];
        FB.Vec3.CreateVec3(builder, v.X, v.Y, v.Z);
      }
      var verticesOffset = builder.EndVector();

      VectorOffset facesOffset = default;
      VectorOffset quadFacesOffset = default;

      if (workingMesh.HasQuads && !workingMesh.HasTriangles) {
        // Pure quad mesh
        FB.MeshData.StartQuadFacesVector(builder, workingMesh.QuadFaces.Length);
        for (int i = workingMesh.QuadFaces.Length - 1; i >= 0; i--) {
          var f = workingMesh.QuadFaces[i];
          FB.Vec4i.CreateVec4i(builder, f.A, f.B, f.C, f.D);
        }
        quadFacesOffset = builder.EndVector();
      } else {
        // Triangle mesh (or mixed, triangulated)
        FB.MeshData.StartFacesVector(builder, workingMesh.TriangleFaces.Length);
        for (int i = workingMesh.TriangleFaces.Length - 1; i >= 0; i--) {
          var f = workingMesh.TriangleFaces[i];
          FB.Vec3i.CreateVec3i(builder, f.A, f.B, f.C);
        }
        facesOffset = builder.EndVector();
      }

      // Create the mesh data
      FB.MeshData.StartMeshData(builder);
      FB.MeshData.AddVertices(builder, verticesOffset);
      if (workingMesh.HasQuads && !workingMesh.HasTriangles) {
        FB.MeshData.AddQuadFaces(builder, quadFacesOffset);
      } else {
        FB.MeshData.AddFaces(builder, facesOffset);
      }
      var meshOffset = FB.MeshData.EndMeshData(builder);
      builder.Finish(meshOffset.Value);

      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to a Mesh.
    /// </summary>
    public static Geometry.Mesh DeserializeMesh(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var meshData = FB.MeshData.GetRootAsMeshData(byteBuffer);

      var mesh = new Geometry.Mesh();

      // Extract vertices
      var vertices = new Geometry.Vec3[meshData.VerticesLength];
      for (int i = 0; i < meshData.VerticesLength; i++) {
        var v = meshData.Vertices(i);
        vertices[i] = v.HasValue
            ? new Geometry.Vec3(v.Value.X, v.Value.Y, v.Value.Z)
            : Geometry.Vec3.Zero;
      }
      mesh.Vertices = vertices;

      // Check for quad faces first
      if (meshData.QuadFacesLength > 0) {
        var quads = new (int, int, int, int)[meshData.QuadFacesLength];
        for (int i = 0; i < meshData.QuadFacesLength; i++) {
          var f = meshData.QuadFaces(i);
          quads[i] = f.HasValue ? (f.Value.X, f.Value.Y, f.Value.Z, f.Value.W) : (0, 0, 0, 0);
        }
        mesh.QuadFaces = quads;
      } else {
        // Triangle faces
        var tris = new (int, int, int)[meshData.FacesLength];
        for (int i = 0; i < meshData.FacesLength; i++) {
          var f = meshData.Faces(i);
          tris[i] = f.HasValue ? (f.Value.X, f.Value.Y, f.Value.Z) : (0, 0, 0);
        }
        mesh.TriangleFaces = tris;
      }

      return mesh;
    }

    #endregion

    #region Primitive Array Serialization

    /// <summary>
    /// Serializes an int array to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(int[] values) {
      var builder = new FlatBufferBuilder(1024);
      var valuesOffset = FB.IntArrayData.CreateValuesVector(builder, values);
      var arrayOffset = FB.IntArrayData.CreateIntArrayData(builder, valuesOffset);
      builder.Finish(arrayOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to an int array.
    /// </summary>
    public static int[] DeserializeIntArray(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var arrayData = FB.IntArrayData.GetRootAsIntArrayData(byteBuffer);

      if (arrayData.ValuesLength == 0)
        return Array.Empty<int>();

      var result = new int[arrayData.ValuesLength];
      for (int i = 0; i < arrayData.ValuesLength; i++) {
        result[i] = arrayData.Values(i);
      }
      return result;
    }

    /// <summary>
    /// Serializes a double array to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(double[] values) {
      var builder = new FlatBufferBuilder(1024);
      var valuesOffset = FB.DoubleArrayData.CreateValuesVector(builder, values);
      var arrayOffset = FB.DoubleArrayData.CreateDoubleArrayData(builder, valuesOffset);
      builder.Finish(arrayOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to a double array.
    /// </summary>
    public static double[] DeserializeDoubleArray(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var arrayData = FB.DoubleArrayData.GetRootAsDoubleArrayData(byteBuffer);

      if (arrayData.ValuesLength == 0)
        return Array.Empty<double>();

      var result = new double[arrayData.ValuesLength];
      for (int i = 0; i < arrayData.ValuesLength; i++) {
        result[i] = arrayData.Values(i);
      }
      return result;
    }

    #endregion

    #region Pair Array Serialization

    /// <summary>
    /// Serializes an array of int pairs to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize((int, int)[] pairs) {
      var builder = new FlatBufferBuilder(1024);

      FB.IntPairArrayData.StartPairsVector(builder, pairs.Length);
      for (int i = pairs.Length - 1; i >= 0; i--) {
        FB.Vec2i.CreateVec2i(builder, pairs[i].Item1, pairs[i].Item2);
      }
      var pairsOffset = builder.EndVector();

      var arrayOffset = FB.IntPairArrayData.CreateIntPairArrayData(builder, pairsOffset);
      builder.Finish(arrayOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to an array of int pairs.
    /// </summary>
    public static (int, int)[] DeserializeIntPairArray(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var pairArray = FB.IntPairArrayData.GetRootAsIntPairArrayData(byteBuffer);

      if (pairArray.PairsLength == 0)
        return Array.Empty<(int, int)>();

      var result = new (int, int)[pairArray.PairsLength];
      for (int i = 0; i < pairArray.PairsLength; i++) {
        var pair = pairArray.Pairs(i);
        result[i] = pair.HasValue ? (pair.Value.X, pair.Value.Y) : (0, 0);
      }
      return result;
    }

    /// <summary>
    /// Serializes an array of double pairs to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize((double, double)[] pairs) {
      var builder = new FlatBufferBuilder(1024);

      FB.DoublePairArrayData.StartPairsVector(builder, pairs.Length);
      for (int i = pairs.Length - 1; i >= 0; i--) {
        FB.Vec2.CreateVec2(builder, pairs[i].Item1, pairs[i].Item2);
      }
      var pairsOffset = builder.EndVector();

      var arrayOffset = FB.DoublePairArrayData.CreateDoublePairArrayData(builder, pairsOffset);
      builder.Finish(arrayOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to an array of double pairs.
    /// </summary>
    public static (double, double)[] DeserializeDoublePairArray(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var pairArray = FB.DoublePairArrayData.GetRootAsDoublePairArrayData(byteBuffer);

      if (pairArray.PairsLength == 0)
        return Array.Empty<(double, double)>();

      var result = new (double, double)[pairArray.PairsLength];
      for (int i = 0; i < pairArray.PairsLength; i++) {
        var pair = pairArray.Pairs(i);
        result[i] = pair.HasValue ? (pair.Value.X, pair.Value.Y) : (0.0, 0.0);
      }
      return result;
    }

    #endregion

    #region Nested Array Serialization

    /// <summary>
    /// Serializes a nested int array (jagged array) to a FlatBuffer byte array.
    /// </summary>
    public static byte[] Serialize(List<List<int>> nestedArray) {
      var builder = new FlatBufferBuilder(1024);

      var flatList = new List<int>();
      var sizes = new List<int>();

      foreach (var subArray in nestedArray) {
        sizes.Add(subArray.Count);
        flatList.AddRange(subArray);
      }

      var valuesOffset = FB.IntNestedArrayData.CreateValuesVector(builder, flatList.ToArray());
      var sizesOffset = FB.IntNestedArrayData.CreateSizesVector(builder, sizes.ToArray());

      var arrayOffset = FB.IntNestedArrayData.CreateIntNestedArrayData(builder, valuesOffset, sizesOffset);
      builder.Finish(arrayOffset.Value);
      return builder.SizedByteArray();
    }

    /// <summary>
    /// Deserializes a FlatBuffer byte array to a nested int array.
    /// </summary>
    public static List<List<int>> DeserializeNestedIntArray(byte[] buffer) {
      var byteBuffer = new ByteBuffer(buffer);
      var arrayData = FB.IntNestedArrayData.GetRootAsIntNestedArrayData(byteBuffer);

      if (arrayData.ValuesLength == 0 || arrayData.SizesLength == 0)
        return new List<List<int>>();

      var result = new List<List<int>>();
      int flatIndex = 0;

      for (int i = 0; i < arrayData.SizesLength; i++) {
        int subArraySize = arrayData.Sizes(i);
        var subArray = new List<int>();

        for (int j = 0; j < subArraySize; j++) {
          if (flatIndex < arrayData.ValuesLength) {
            subArray.Add(arrayData.Values(flatIndex++));
          }
        }
        result.Add(subArray);
      }

      return result;
    }

    #endregion
  }
}

using Google.FlatBuffers;
using Rhino.Geometry;

namespace GSP.Adapters.Rhino {
public static class Wrapper {
#region Point3d / Vector3d Operations

  // Single Point/Vector IO
  public static byte[] ToPointBuffer(Point3d pt) {
    var builder = new FlatBufferBuilder(64);  // Enough for a single Vector3d

    // Create a Vector3d in the FlatBuffer
    FB.PointData.StartPointData(builder);
    var vecOffset = FB.Vec3.CreateVec3(builder, pt.X, pt.Y, pt.Z);
    FB.PointData.AddPoint(builder, vecOffset);
    var ptOffset = FB.PointData.EndPointData(builder);

    // Finish the buffer with the root table offset
    builder.Finish(ptOffset.Value);

    // Now get the completed buffer
    return builder.SizedByteArray();
  }

  public static byte[] ToVector3dBuffer(Vector3d vector) {
    return ToPointBuffer(new Point3d(vector));
  }

  public static Point3d FromPointBuffer(byte[] buffer) {
    var ptByteBuffer = new ByteBuffer(buffer);
    var pt = FB.PointData.GetRootAsPointData(ptByteBuffer).Point;

    return pt.HasValue ? new Point3d(pt.Value.X, pt.Value.Y, pt.Value.Z)
                       : new Point3d(0, 0, 0);  // Default value if null
  }

  public static Vector3d FromVector3dBuffer(byte[] buffer) {
    return new Vector3d(FromPointBuffer(buffer));
  }

  // Point/Vector Array IO
  public static byte[] ToPointArrayBuffer(Point3d[] points) {
    var builder = new FlatBufferBuilder(1024);

    // Add points in reverse order (FlatBuffers build vectors backwards) to build the point vector
    FB.PointArrayData.StartPointsVector(builder, points.Length);
    for (int i = points.Length - 1; i >= 0; i--) {
      FB.Vec3.CreateVec3(builder, points[i].X, points[i].Y, points[i].Z);
    }
    var ptOffset = builder.EndVector();

    var arrayOffset = FB.PointArrayData.CreatePointArrayData(builder, ptOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static byte[] ToPointArrayBuffer(List<Point3d> points) {
    return ToPointArrayBuffer(points.ToArray());
  }

  public static byte[] ToVector3dArrayBuffer(Vector3d[] vectors) {
    var points = vectors.Select(v => new Point3d(v)).ToArray();
    return ToPointArrayBuffer(points);
  }

  public static byte[] ToPointArrayBuffer(List<Vector3d> vectors) {
    var points = vectors.Select(v => new Point3d(v)).ToArray();
    return ToPointArrayBuffer(points);
  }

  public static Point3d[] FromPointArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var pointArray = FB.PointArrayData.GetRootAsPointArrayData(byteBuffer);

    var pts = pointArray.Points;
    // 1. Check if the array is valid
    if (pointArray.PointsLength == 0)
      return Array.Empty<Point3d>();

    // 2. Extract structs directly
    var res = new Point3d[pointArray.PointsLength];
    for (int i = 0; i < pointArray.PointsLength; i++) {
      var pt = pointArray.Points(i);
      res[i] = pt.HasValue ? new Point3d(pt.Value.X, pt.Value.Y, pt.Value.Z)
                           : new Point3d(0, 0, 0);  // Default value if null
    }
    return res;
  }

  public static Vector3d[] FromVector3dArrayBuffer(byte[] buffer) {
    var vectors = FromPointArrayBuffer(buffer);
    return vectors.Select(vectors => new Vector3d(vectors)).ToArray();
  }

  public static List<Point3d>
      FromPointArrayBufferToList(byte[] buffer) => new List<Point3d>(FromPointArrayBuffer(buffer));

  public static List<Vector3d> FromVector3dArrayBufferToList(byte[] buffer) =>
      new List<Vector3d>(FromVector3dArrayBuffer(buffer));

#endregion

#region Mesh Operations

  public static byte[] ToMeshBuffer(Mesh mesh, bool preserveQuads = false) {
    var builder = new FlatBufferBuilder(1024);

    // Check if mesh has quads
    bool hasQuads = false;
    bool hasTriangles = false;

    foreach (var face in mesh.Faces) {
      if (face.IsTriangle) {
        hasTriangles = true;
      } else {
        hasQuads = true;
      }

      // If we find a mixed mesh, break early
      if (hasQuads && hasTriangles) {
        break;
      }
    }

    Mesh workingMesh = mesh;

    // Strategy:
    // - If mixed mesh OR (has quads AND not preserving), triangulate
    // - Otherwise keep as-is
    bool isMixed = hasQuads && hasTriangles;
    if (isMixed || (hasQuads && !preserveQuads)) {
      workingMesh = mesh.DuplicateMesh();
      workingMesh.Faces.ConvertQuadsToTriangles();
      hasQuads = false;
      hasTriangles = true;
    }

    // Add vertices
    FB.MeshData.StartVerticesVector(builder, workingMesh.Vertices.Count);
    for (int i = workingMesh.Vertices.Count - 1; i >= 0; i--) {
      var vertex = workingMesh.Vertices[i];
      FB.Vec3.CreateVec3(builder, vertex.X, vertex.Y, vertex.Z);
    }
    var verticesOffset = builder.EndVector();

    VectorOffset facesOffset = default;
    VectorOffset quadFacesOffset = default;

    if (hasQuads && !hasTriangles) {
      // Pure quad mesh
      FB.MeshData.StartQuadFacesVector(builder, workingMesh.Faces.Count);
      for (int i = workingMesh.Faces.Count - 1; i >= 0; i--) {
        var face = workingMesh.Faces[i];
        FB.Vec4i.CreateVec4i(builder, face.A, face.B, face.C, face.D);
      }
      quadFacesOffset = builder.EndVector();
    } else {
      // Pure triangle mesh (or was converted to triangles)
      FB.MeshData.StartFacesVector(builder, workingMesh.Faces.Count);
      for (int i = workingMesh.Faces.Count - 1; i >= 0; i--) {
        var face = workingMesh.Faces[i];
        FB.Vec3i.CreateVec3i(builder, face.A, face.B, face.C);
      }
      facesOffset = builder.EndVector();
    }

    // Create the mesh data
    FB.MeshData.StartMeshData(builder);
    FB.MeshData.AddVertices(builder, verticesOffset);
    if (hasQuads && !hasTriangles) {
      FB.MeshData.AddQuadFaces(builder, quadFacesOffset);
    } else {
      FB.MeshData.AddFaces(builder, facesOffset);
    }
    var meshOffset = FB.MeshData.EndMeshData(builder);
    builder.Finish(meshOffset.Value);

    return builder.SizedByteArray();
  }

  public static Mesh FromMeshBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var meshData = FB.MeshData.GetRootAsMeshData(byteBuffer);

    var mesh = new Mesh();

    // Add vertices
    for (int i = 0; i < meshData.VerticesLength; i++) {
      var vertex = meshData.Vertices(i);
      if (vertex.HasValue) {
        mesh.Vertices.Add(vertex.Value.X, vertex.Value.Y, vertex.Value.Z);
      }
    }

    // Check if we have quad faces
    bool hasQuadFaces = meshData.QuadFacesLength > 0;

    if (hasQuadFaces) {
      // Add quad faces
      for (int i = 0; i < meshData.QuadFacesLength; i++) {
        var face = meshData.QuadFaces(i);
        if (face.HasValue) {
          mesh.Faces.AddFace(face.Value.X, face.Value.Y, face.Value.Z, face.Value.W);
        }
      }
    } else {
      // Add triangle faces
      for (int i = 0; i < meshData.FacesLength; i++) {
        var face = meshData.Faces(i);
        if (face.HasValue) {
          mesh.Faces.AddFace(face.Value.X, face.Value.Y, face.Value.Z);
        }
      }
    }

    if (mesh.IsValid) {
      mesh.RebuildNormals();
      mesh.Compact();
    }

    return mesh;
  }

#endregion

#region Int Array Operations

  public static byte[] ToIntArrayBuffer(int[] values) {
    var builder = new FlatBufferBuilder(1024);

    // Create the values vector
    var valuesOffset = FB.IntArrayData.CreateValuesVector(builder, values);

    // Create the array data
    var arrayOffset = FB.IntArrayData.CreateIntArrayData(builder, valuesOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static byte[] ToIntArrayBuffer(List<int> values) => ToIntArrayBuffer(values.ToArray());

  public static int[] FromIntArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var arrayData = FB.IntArrayData.GetRootAsIntArrayData(byteBuffer);

    // Check if the array is valid
    if (arrayData.ValuesLength == 0)
      return Array.Empty<int>();

    // Extract values
    var result = new int[arrayData.ValuesLength];
    for (int i = 0; i < arrayData.ValuesLength; i++) {
      result[i] = arrayData.Values(i);
    }
    return result;
  }

  public static List<int>
      FromIntArrayBufferToList(byte[] buffer) => new List<int>(FromIntArrayBuffer(buffer));

#endregion

#region Int Pair Array Operations

  public static byte[] ToIntPairArrayBuffer((int, int)[] pairs) {
    var builder = new FlatBufferBuilder(1024);

    // Add pairs in reverse order (FlatBuffers build vectors backwards)
    FB.IntPairArrayData.StartPairsVector(builder, pairs.Length);
    for (int i = pairs.Length - 1; i >= 0; i--) {
      FB.Vec2i.CreateVec2i(builder, pairs[i].Item1, pairs[i].Item2);
    }
    var pairsOffset = builder.EndVector();

    var arrayOffset = FB.IntPairArrayData.CreateIntPairArrayData(builder, pairsOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static byte[] ToIntPairArrayBuffer(List<(int, int)> pairs) =>
      ToIntPairArrayBuffer(pairs.ToArray());

  public static (int, int)[] FromIntPairArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var pairArray = FB.IntPairArrayData.GetRootAsIntPairArrayData(byteBuffer);

    // Check if the array is valid
    if (pairArray.PairsLength == 0)
      return Array.Empty<(int, int)>();

    // Extract pairs
    var result = new(int, int)[pairArray.PairsLength];
    for (int i = 0; i < pairArray.PairsLength; i++) {
      var pair = pairArray.Pairs(i);
      result[i] = pair.HasValue ? (pair.Value.X, pair.Value.Y) : (0, 0);  // Default value if null
    }
    return result;
  }

  public static List<(int, int)> FromIntPairArrayBufferToList(byte[] buffer) =>
      new List<(int, int)>(FromIntPairArrayBuffer(buffer));

#endregion

#region Double Array Operations

  public static byte[] ToDoubleArrayBuffer(double[] values) {
    var builder = new FlatBufferBuilder(1024);

    // Create the values vector
    var valuesOffset = FB.DoubleArrayData.CreateValuesVector(builder, values);

    // Create the array data
    var arrayOffset = FB.DoubleArrayData.CreateDoubleArrayData(builder, valuesOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static byte[] ToDoubleArrayBuffer(List<double> values) =>
      ToDoubleArrayBuffer(values.ToArray());

  public static double[] FromDoubleArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var arrayData = FB.DoubleArrayData.GetRootAsDoubleArrayData(byteBuffer);

    // Check if the array is valid
    if (arrayData.ValuesLength == 0)
      return Array.Empty<double>();

    // Extract values
    var result = new double[arrayData.ValuesLength];
    for (int i = 0; i < arrayData.ValuesLength; i++) {
      result[i] = arrayData.Values(i);
    }
    return result;
  }

  public static List<double>
      FromDoubleArrayBufferToList(byte[] buffer) => new List<double>(FromDoubleArrayBuffer(buffer));

#endregion

#region Double Pair Array Operations

  public static byte[] ToDoublePairArrayBuffer((double, double)[] pairs) {
    var builder = new FlatBufferBuilder(1024);

    // Add pairs in reverse order (FlatBuffers build vectors backwards)
    FB.DoublePairArrayData.StartPairsVector(builder, pairs.Length);
    for (int i = pairs.Length - 1; i >= 0; i--) {
      FB.Vec2.CreateVec2(builder, pairs[i].Item1, pairs[i].Item2);
    }
    var pairsOffset = builder.EndVector();

    var arrayOffset = FB.DoublePairArrayData.CreateDoublePairArrayData(builder, pairsOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static byte[] ToDoublePairArrayBuffer(List<(double, double)> pairs) =>
      ToDoublePairArrayBuffer(pairs.ToArray());

  public static (double, double)[] FromDoublePairArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var pairArray = FB.DoublePairArrayData.GetRootAsDoublePairArrayData(byteBuffer);

    // Check if the array is valid
    if (pairArray.PairsLength == 0)
      return Array.Empty<(double, double)>();

    // Extract pairs
    var result = new(double, double)[pairArray.PairsLength];
    for (int i = 0; i < pairArray.PairsLength; i++) {
      var pair = pairArray.Pairs(i);
      result[i] =
          pair.HasValue ? (pair.Value.X, pair.Value.Y) : (0.0, 0.0);  // Default value if null
    }
    return result;
  }

  public static List<(double, double)> FromDoublePairArrayBufferToList(byte[] buffer) =>
      new List<(double, double)>(FromDoublePairArrayBuffer(buffer));

#endregion

#region Nested Int Array Operations

  public static byte[] ToNestedIntArrayBuffer(List<List<int>> nestedArray) {
    var builder = new FlatBufferBuilder(1024);

    // Flatten the nested array and keep track of sizes
    var flatList = new List<int>();
    var sizes = new List<int>();

    foreach (var subArray in nestedArray) {
      sizes.Add(subArray.Count);
      flatList.AddRange(subArray);
    }

    // Create the values and sizes vectors
    var valuesOffset = FB.IntNestedArrayData.CreateValuesVector(builder, flatList.ToArray());
    var sizesOffset = FB.IntNestedArrayData.CreateSizesVector(builder, sizes.ToArray());

    // Create the nested array data
    var arrayOffset =
        FB.IntNestedArrayData.CreateIntNestedArrayData(builder, valuesOffset, sizesOffset);
    builder.Finish(arrayOffset.Value);

    return builder.SizedByteArray();
  }

  public static List<List<int>> FromNestedIntArrayBuffer(byte[] buffer) {
    var byteBuffer = new ByteBuffer(buffer);
    var arrayData = FB.IntNestedArrayData.GetRootAsIntNestedArrayData(byteBuffer);

    // Check if the array is valid
    if (arrayData.ValuesLength == 0 || arrayData.SizesLength == 0)
      return new List<List<int>>();

    var result = new List<List<int>>();
    int flatIndex = 0;

    // Reconstruct the nested structure
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

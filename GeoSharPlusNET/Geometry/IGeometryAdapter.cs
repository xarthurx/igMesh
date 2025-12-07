namespace GSP.Geometry {
  /// <summary>
  /// Interface for adapting CAD-specific geometry types to GSP geometry types.
  /// Implement this interface to add support for a new CAD platform.
  /// </summary>
  /// <typeparam name="TPoint">The CAD platform's point/vector type.</typeparam>
  /// <typeparam name="TMesh">The CAD platform's mesh type.</typeparam>
  /// <remarks>
  /// Example implementation for Rhino:
  /// <code>
  /// public class RhinoAdapter : IGeometryAdapter&lt;Point3d, Rhino.Geometry.Mesh&gt; {
  ///     public Vec3 PointToGSP(Point3d point) => new Vec3(point.X, point.Y, point.Z);
  ///     public Point3d PointFromGSP(Vec3 point) => new Point3d(point.X, point.Y, point.Z);
  ///     // ... mesh methods
  /// }
  /// </code>
  /// 
  /// This allows the library to support multiple CAD platforms (Rhino, AutoCAD, Revit, etc.)
  /// without changing the core serialization logic.
  /// </remarks>
  public interface IGeometryAdapter<TPoint, TMesh> {
    /// <summary>
    /// Converts a CAD point to a GSP Vec3.
    /// </summary>
    Vec3 PointToGSP(TPoint point);

    /// <summary>
    /// Converts a GSP Vec3 to a CAD point.
    /// </summary>
    TPoint PointFromGSP(Vec3 point);

    /// <summary>
    /// Converts an array of CAD points to GSP Vec3 array.
    /// </summary>
    Vec3[] PointsToGSP(TPoint[] points);

    /// <summary>
    /// Converts a GSP Vec3 array to CAD points.
    /// </summary>
    TPoint[] PointsFromGSP(Vec3[] points);

    /// <summary>
    /// Converts a CAD mesh to a GSP Mesh.
    /// </summary>
    Mesh MeshToGSP(TMesh mesh);

    /// <summary>
    /// Converts a GSP Mesh to a CAD mesh.
    /// </summary>
    TMesh MeshFromGSP(Mesh mesh);
  }
}

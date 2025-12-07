using System;

namespace GSP.Geometry {
  /// <summary>
  /// A simple 3D vector/point structure.
  /// Platform-independent - no CAD software dependencies.
  /// </summary>
  public readonly struct Vec3 : IEquatable<Vec3> {
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Vec3(double x, double y, double z) {
      X = x;
      Y = y;
      Z = z;
    }

    /// <summary>
    /// Creates a Vec3 from an array of 3 doubles.
    /// </summary>
    public Vec3(double[] values) {
      if (values == null || values.Length < 3)
        throw new ArgumentException("Array must have at least 3 elements", nameof(values));
      X = values[0];
      Y = values[1];
      Z = values[2];
    }

    /// <summary>
    /// Returns the zero vector (0, 0, 0).
    /// </summary>
    public static Vec3 Zero => new(0, 0, 0);

    /// <summary>
    /// Returns the unit X vector (1, 0, 0).
    /// </summary>
    public static Vec3 UnitX => new(1, 0, 0);

    /// <summary>
    /// Returns the unit Y vector (0, 1, 0).
    /// </summary>
    public static Vec3 UnitY => new(0, 1, 0);

    /// <summary>
    /// Returns the unit Z vector (0, 0, 1).
    /// </summary>
    public static Vec3 UnitZ => new(0, 0, 1);

    /// <summary>
    /// Returns the length (magnitude) of the vector.
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns the squared length of the vector (faster than Length).
    /// </summary>
    public double LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Returns a normalized (unit length) version of this vector.
    /// </summary>
    public Vec3 Normalized {
      get {
        var len = Length;
        return len > 0 ? new Vec3(X / len, Y / len, Z / len) : Zero;
      }
    }

    /// <summary>
    /// Converts to an array of doubles [X, Y, Z].
    /// </summary>
    public double[] ToArray() => new[] { X, Y, Z };

    // Operators
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 v, double s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vec3 operator *(double s, Vec3 v) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vec3 operator /(Vec3 v, double s) => new(v.X / s, v.Y / s, v.Z / s);
    public static Vec3 operator -(Vec3 v) => new(-v.X, -v.Y, -v.Z);

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>
    /// Computes the cross product of two vectors.
    /// </summary>
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    public static double Distance(Vec3 a, Vec3 b) => (a - b).Length;

    // Equality
    public bool Equals(Vec3 other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Vec3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Vec3 left, Vec3 right) => left.Equals(right);
    public static bool operator !=(Vec3 left, Vec3 right) => !left.Equals(right);

    /// <summary>
    /// Checks if two vectors are approximately equal within a tolerance.
    /// </summary>
    public bool ApproximatelyEquals(Vec3 other, double tolerance = 1e-9) =>
        Math.Abs(X - other.X) < tolerance &&
        Math.Abs(Y - other.Y) < tolerance &&
        Math.Abs(Z - other.Z) < tolerance;

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
  }
}

using System;

namespace GSP.Geometry {
  /// <summary>
  /// A simple 2D vector/point structure.
  /// Platform-independent - no CAD software dependencies.
  /// </summary>
  public readonly struct Vec2 : IEquatable<Vec2> {
    public double X { get; }
    public double Y { get; }

    public Vec2(double x, double y) {
      X = x;
      Y = y;
    }

    /// <summary>
    /// Creates a Vec2 from an array of 2 doubles.
    /// </summary>
    public Vec2(double[] values) {
      if (values == null || values.Length < 2)
        throw new ArgumentException("Array must have at least 2 elements", nameof(values));
      X = values[0];
      Y = values[1];
    }

    /// <summary>
    /// Returns the zero vector (0, 0).
    /// </summary>
    public static Vec2 Zero => new(0, 0);

    /// <summary>
    /// Returns the unit X vector (1, 0).
    /// </summary>
    public static Vec2 UnitX => new(1, 0);

    /// <summary>
    /// Returns the unit Y vector (0, 1).
    /// </summary>
    public static Vec2 UnitY => new(0, 1);

    /// <summary>
    /// Returns the length (magnitude) of the vector.
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Returns the squared length of the vector (faster than Length).
    /// </summary>
    public double LengthSquared => X * X + Y * Y;

    /// <summary>
    /// Returns a normalized (unit length) version of this vector.
    /// </summary>
    public Vec2 Normalized {
      get {
        var len = Length;
        return len > 0 ? new Vec2(X / len, Y / len) : Zero;
      }
    }

    /// <summary>
    /// Converts to an array of doubles [X, Y].
    /// </summary>
    public double[] ToArray() => new[] { X, Y };

    /// <summary>
    /// Converts to a Vec3 with Z = 0.
    /// </summary>
    public Vec3 ToVec3(double z = 0) => new(X, Y, z);

    // Operators
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, double s) => new(v.X * s, v.Y * s);
    public static Vec2 operator *(double s, Vec2 v) => new(v.X * s, v.Y * s);
    public static Vec2 operator /(Vec2 v, double s) => new(v.X / s, v.Y / s);
    public static Vec2 operator -(Vec2 v) => new(-v.X, -v.Y);

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>
    /// Computes the 2D cross product (returns scalar).
    /// </summary>
    public static double Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;

    // Equality
    public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Vec2 left, Vec2 right) => left.Equals(right);
    public static bool operator !=(Vec2 left, Vec2 right) => !left.Equals(right);

    /// <summary>
    /// Checks if two vectors are approximately equal within a tolerance.
    /// </summary>
    public bool ApproximatelyEquals(Vec2 other, double tolerance = 1e-9) =>
        Math.Abs(X - other.X) < tolerance &&
        Math.Abs(Y - other.Y) < tolerance;

    public override string ToString() => $"({X:F4}, {Y:F4})";
  }
}

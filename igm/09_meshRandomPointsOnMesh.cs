using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Windows.Forms;

namespace igm {
public class IGM_random_points : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_random_points()
      : base("Random Points",
             "igRndPt",
             "Randomly sample N points on surface of the given mesh, Brep, or planar " +
                 "closed curve with random/uniform " + "distribution.",
             "igMesh",
             "09::Utils") {}

  /// <summary>
  /// icon position in a category
  /// </summary>
  public override GH_Exposure Exposure => GH_Exposure.secondary;

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddGeometryParameter(
        "Geometry",
        "G",
        "Input geometry for analysis: mesh, Brep, NURBS surface, or planar closed curve.",
        GH_ParamAccess.item);
    pManager.AddIntegerParameter(
        "Number", "N", "Number of sampled points.", GH_ParamAccess.item, 100);
    pManager[1].Optional = true;
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddPointParameter("Sampled Points", "P", "The N sampled points.", GH_ParamAccess.list);
    pManager.AddIntegerParameter("Face Index",
                                 "FI",
                                 "The corresponding face indices of the sampled points.",
                                 GH_ParamAccess.list);
  }

  /// <summary>
  /// Converts various geometry types to mesh
  /// </summary>
  private Mesh ConvertGeometryToMesh(GeometryBase geometry) {
    switch (geometry) {
      case Mesh mesh:
        return mesh;

      case Brep brep:
        // Convert Brep (which includes NURBS surfaces) to mesh
        var brepMeshParams = MeshingParameters.Default;
        brepMeshParams.RelativeTolerance = 0.1;
        brepMeshParams.MinimumEdgeLength = 0.01;

        var brepMeshes = Mesh.CreateFromBrep(brep, brepMeshParams);
        if (brepMeshes != null && brepMeshes.Length > 0) {
          var combinedBrepMesh = new Mesh();
          foreach (var m in brepMeshes) {
            combinedBrepMesh.Append(m);
          }
          return combinedBrepMesh;
        }
        break;

      case Curve curve when curve.IsClosed && curve.IsPlanar():
        // Convert planar closed curve to mesh
        if (curve.TryGetPlane(out Plane plane)) {
          // Try to convert curve to polyline and create mesh
          if (curve.TryGetPolyline(out Polyline polyline)) {
            return Mesh.CreateFromClosedPolyline(polyline);
          } else {
            // For complex curves, discretize and create fan triangulation
            var points = curve.DivideByCount(50, true);
            if (points != null && points.Length > 3) {
              var meshFromCurve = new Mesh();

              // Add vertices from curve points
              for (int i = 0; i < points.Length; i++) {
                meshFromCurve.Vertices.Add(curve.PointAt(points[i]));
              }

              // Calculate centroid for fan triangulation
              var centroid = Point3d.Origin;
              foreach (var vertex in meshFromCurve.Vertices) {
                centroid += vertex;
              }
              centroid /= meshFromCurve.Vertices.Count;
              meshFromCurve.Vertices.Add(centroid);
              int centerIndex = meshFromCurve.Vertices.Count - 1;

              // Create triangular faces from center to each edge
              for (int i = 0; i < points.Length - 1; i++) {
                meshFromCurve.Faces.AddFace(centerIndex, i, i + 1);
              }
              // Close the loop
              meshFromCurve.Faces.AddFace(centerIndex, points.Length - 1, 0);

              return meshFromCurve.IsValid ? meshFromCurve : null;
            }
          }
        }
        break;
    }

    return null;
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA) {
    GeometryBase inputGeometry = null;
    if (!DA.GetData(0, ref inputGeometry)) {
      return;
    }

    // Convert input geometry to mesh
    Mesh mesh = ConvertGeometryToMesh(inputGeometry);
    if (mesh == null || !mesh.IsValid) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        "Failed to convert input geometry to mesh or resulting mesh is invalid.");
      return;
    }

    int N = 1;
    if (!DA.GetData(1, ref N) || N == 0) {
      return;
    }

    int M = (mode == "random" ? 0 : 1);
    // if (!DA.GetData(2, ref M))
    //{ return; }

    // call the GeoSharPlusNET function to sample points on mesh
    var (p, fi) = MeshUtils.GetRandomPointsOnMesh(ref mesh, N, M);

    // output
    DA.SetDataList(0, p);
    DA.SetDataList(1, fi);
  }

  protected override void BeforeSolveInstance() {
    Message = mode.ToUpper();
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu) {
    base.AppendAdditionalMenuItems(menu);

    Menu_AppendSeparator(menu);
    Menu_AppendItem(menu, "Mode:", (sender, e) => {}, false).Font = GH_FontServer.StandardItalic;
    Menu_AppendItem(menu,
                    " Random",
                    (sender, e) => Helper.SelectMode(this, sender, e, ref mode, "random"),
                    true,
                    CheckMode("random"));
    Menu_AppendItem(menu,
                    " Uniform",
                    (sender, e) => Helper.SelectMode(this, sender, e, ref mode, "uniform"),
                    true,
                    CheckMode("uniform"));
  }

  public override bool Write(GH_IO.Serialization.GH_IWriter writer) {
    if (mode != "")
      writer.SetString("mode", mode);

    return base.Write(writer);
  }
  public override bool Read(GH_IO.Serialization.GH_IReader reader) {
    if (reader.ItemExists("mode"))
      mode = reader.GetString("mode");

    Message = reader.GetString("mode").ToUpper();

    return base.Read(reader);
  }

  private bool CheckMode(string _modeCheck) => mode == _modeCheck;

  private string mode = "random";  // "random" or "uniform"

  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshRandomPtsOnMesh;
  public override Guid ComponentGuid => new Guid("5819dc11-ccff-41eb-b126-96c34911ddc1");
}
}



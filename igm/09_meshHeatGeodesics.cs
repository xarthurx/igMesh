using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;

namespace igm {
public class IGM_heat_geodesic_dist : GH_Component {
  Rhino.Geometry.Mesh heat_mesh;
  static long geoDataHandle;

  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_heat_geodesic_dist()
      : base("HeatGeo Distance",
             "igGeoDist",
             "Compute geodesic distance of multiple sources on a mesh using the " +
                 "heat-transfer-inspired fast approximation method.",
             "igMesh",
             "06::measure") {}

  /// <summary>
  /// icon position in a category
  /// </summary>
  public override GH_Exposure Exposure => GH_Exposure.quarternary;

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddIntegerParameter(
        "Source", "S", "Source vertex list to compute distance from.", GH_ParamAccess.list);
    pManager.AddBooleanParameter(
        "Re-compute", "B", "Recompute the whole process.", GH_ParamAccess.item, false);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddNumberParameter(
        "Distance", "D", "The computed distance list of each vertex.", GH_ParamAccess.list);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    List<int> gamma = new List<int>();
    if (!DA.GetDataList(1, gamma) || gamma.Count == 0) {
      return;
    }

    bool redo = false;
    if (!DA.GetData(2, ref redo)) {
      return;
    }

    var meshSame = (heat_mesh != null &&
                    Rhino.Geometry.InstanceReferenceGeometry.GeometryEquals(mesh, heat_mesh));
    if (redo || geoDataHandle == 0 || !meshSame) {
      geoDataHandle = MeshUtils.GetHeatGeodesicPrecomputedData(ref mesh);
      heat_mesh = mesh;
    }

    var D = MeshUtils.GetHeatGeodesicDistances(geoDataHandle, ref gamma);

    // output
    DA.SetDataList(0, D);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      //  return Resources.IconForThisComponent;
      return Properties.Resources.meshHeatGeodesic;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("449dc8bc-4b60-412e-aa5e-9fea06532c6d"); }
  }
}
}



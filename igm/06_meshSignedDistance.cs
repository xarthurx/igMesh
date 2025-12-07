using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace igm {
public class IGM_signed_distance : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_signed_distance()
      : base("Signed Distance",
             "igSignedDist",
             "Compute the signed distance for the query pts to the given mesh.",
             "igMesh",
             "06::measure") {}

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddPointParameter(
        "QueryPoints", "P", "The points to be queried.", GH_ParamAccess.list);
    pManager.AddIntegerParameter("signed_type",
                                 "st",
                                 "The method used for computing signed distance: 1-winding " +
                                 "number; 2-default; 3-unsigned; 4-fast winding number (default).",
                                 GH_ParamAccess.item,
                                 4);
    pManager[2].Optional = true;
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddNumberParameter("Signed Distance",
                                "SD",
                                "The smallest signed distances of the queried points.",
                                GH_ParamAccess.list);
    pManager.AddIntegerParameter("Closest Face Index",
                                 "FI",
                                 "Face indices corresponding to the smallest distances.",
                                 GH_ParamAccess.list);
    pManager.AddPointParameter(
        "Closest Point", "CP", "Closest Points to the queried points.", GH_ParamAccess.list);
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

    List<Rhino.Geometry.Point3d> Q = new List<Rhino.Geometry.Point3d>();
    if (!DA.GetDataList(1, Q)) {
      return;
    }

    int st = 4;
    if (!DA.GetData(2, ref st)) {}

    // call the cpp function to solve the adjacency list
    var (sd, fi, cp) = MeshUtils.GetSignedDistance(ref mesh, ref Q, st);

    // output
    DA.SetDataList(0, sd);
    DA.SetDataList(1, fi);
    DA.SetDataList(2, cp);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      return Properties.Resources.meshSignedDist;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("864f0192-bba2-487c-85ec-0118e564d69d"); }
  }
}
}



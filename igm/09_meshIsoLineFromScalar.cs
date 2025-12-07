using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace igm {
public class IGM_isoline_scalar : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_isoline_scalar()
      : base("Isoline",
             "igIsoline",
             "Extract the isolines of a given mesh from its scalar field.",
             "igMesh",
             "09::Utils") {}

  /// <summary>
  /// icon position in a category
  /// </summary>
  public override GH_Exposure Exposure => GH_Exposure.primary;

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddNumberParameter(
        "Mesh Scalar", "S", "Scalar values for the vertices.", GH_ParamAccess.list);
    pManager.AddNumberParameter("Isoline t",
                                "t",
                                "Interpretation parameters (in [0, 1]) of isolines.",
                                GH_ParamAccess.list);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddPointParameter(
        "Isoline Point", "P", "Extracted points on isolines.", GH_ParamAccess.tree);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA) {
    Mesh mesh = new Mesh();
    List<double> mesh_scalar = new List<double>();
    List<double> iso_t = new List<double>();

    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    if (!DA.GetDataList(1, mesh_scalar)) {
      return;
    }
    if (!DA.GetDataList(2, iso_t)) {
      return;
    }

    foreach (var t in iso_t) {
      if (t < 0 || t > 1)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                          "parameter t should be in the range [0, 1].");
    }

    // call the GeoSharPlusNET function to extract isolines
    var isoPts = MeshUtils.GetIsolineFromScalar(ref mesh, ref mesh_scalar, ref iso_t);

    // construct the index & pt tree from the isoline points
    Grasshopper.DataTree<Point3d> ptTree = new Grasshopper.DataTree<Point3d>();
    for (int i = 0; i < isoPts.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      ptTree.AddRange(isoPts[i], path);
    }

    // assign to the output
    DA.SetDataTree(0, ptTree);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      return Properties.Resources.meshIsoline;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("e70efc9e-e42f-453d-bcdf-e73dbdddbf70"); }
  }
}
}



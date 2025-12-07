using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace igm {
public class IGM_laplacian : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_laplacian()
      : base("Laplacian",
             "igLaplacian",
             "Solve laplacian equation under given boundary condition.",
             "IG-Mesh",
             "06 | Utils") {}

  // icon position in a category
  public override GH_Exposure Exposure => GH_Exposure.secondary;

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddIntegerParameter(
        "Constraint Indices", "I", "the indices to be constrained", GH_ParamAccess.list);
    pManager.AddNumberParameter(
        "Constraint Values", "V", "the values to constrain with", GH_ParamAccess.list);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    // pManager.AddMeshParameter("Mesh", "M", "output mesh with color info.", GH_ParamAccess.item);
    pManager.AddNumberParameter(
        "Scalar Value", "D", "scalar value for all vertices.", GH_ParamAccess.list);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    List<int> con_idx = new List<int>();
    List<double> con_val = new List<double>();

    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    if (!DA.GetDataList(1, con_idx)) {
      return;
    }
    if (!DA.GetDataList(2, con_val)) {
      return;
    }
    if (!(con_idx.Count > 0) || !(con_val.Count > 0)) {
      return;
    }
    if (con_idx.Count != con_val.Count) {
      return;
    }

    // call the cpp function to solve the adjacency list
    var res = MeshUtils.GetLaplacianScalar(ref mesh, ref con_idx, ref con_val);

    DA.SetDataList(0, res);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      //  return Resources.IconForThisComponent;
      return null;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("9a5af6ef-c8fd-4e0f-9e70-84b709f53be7"); }
  }
}
}



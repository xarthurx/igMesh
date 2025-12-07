using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace igm {
public class IGM_winding_number : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_winding_number()
      : base("Winding Number",
             "igWindingNum",
             "Compute the winding number for the query pts to the given mesh.",
             "igMesh",
             "06::measure") {}

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddPointParameter(
        "QueryPoints", "P", "The points to be queried.", GH_ParamAccess.list);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddNumberParameter(
        "Winding Number", "W", "The winding number of the queried points.", GH_ParamAccess.list);
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

    // call the cpp function to solve the adjacency list
    var w = MeshUtils.GetFastWindingNumber(ref mesh, ref Q);

    // output
    DA.SetDataList(0, w);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshWindingNumber;
  //{
  //    get
  //    {
  //        //You can add image files to your project resources and access them like this:
  //        // return Resources.IconForThisComponent;
  //        return Properties.Resources.meshWindingNumber;
  //    }
  //}

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("c5c7b5c0-2cb4-48e6-903d-0e186c6de25d"); }
  }
}
}



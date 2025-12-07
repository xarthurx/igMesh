using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace igm {
public class IGM_constrained_scalar : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_constrained_scalar()
      : base("Constrained Scalar",
             "igConstrainedScalar",
             "Compute a scalar field based on any vertex-based constraints.",
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
    pManager.AddIntegerParameter(
        "Constraint Index", "conI", "Vertex indices to be constrained.", GH_ParamAccess.list);
    pManager.AddNumberParameter(
        "Constraint Value", "conV", "Values (in [0, 1]) to constrain with.", GH_ParamAccess.list);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddNumberParameter(
        "Mesh Scalar", "S", "Scalar values for the vertices.", GH_ParamAccess.list);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA) {
    Mesh mesh = new Mesh();
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

    if (con_idx.Count <= 0 || con_val.Count <= 0) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "# of vertices and constrained values need to be > 0.");
      return;
    }

    if (con_idx.Count != con_val.Count) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        "# of vertices and # of corresponding constrained values do not match.");
      return;
    }

    foreach (var val in con_val) {
      if (val < 0 || val > 1) {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "constrained value should be in [0, 1].");
        return;
      }
    }

    // call the GeoSharPlusNET function to solve the constrained scalar field
    var scalarV = MeshUtils.GetConstrainedScalar(ref mesh, ref con_idx, ref con_val);

    // assign to the output
    DA.SetDataList(0, scalarV);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get { return Properties.Resources.meshConstrainedScalar; }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("35e7181a-b972-11ec-8422-0242ac120002"); }
  }
}
}



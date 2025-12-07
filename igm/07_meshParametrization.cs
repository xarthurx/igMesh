using Grasshopper.Kernel;
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;

namespace igm {
public class IGM_paramHarmonic : GH_Component {
  /// <summary>
  /// Harmonic parametrization
  /// https://libigl.github.io/tutorial/#harmonic-parameterization
  /// </summary>
  public IGM_paramHarmonic()
      : base("Harmonic Parametrization.",
             "igParamHarmonic",
             "Harmonic parametrization of a mesh into a circular shape.",
             "igMesh",
             "07::parametrization") {}

  // public override GH_Exposure Exposure => GH_Exposure.primary;
  public override GH_Exposure Exposure => GH_Exposure.hidden;
  public override Guid ComponentGuid => new Guid("bc1ad1ab-be4f-4b3e-9beb-b15729db8b53");
  // protected override System.Drawing.Bitmap Icon => Properties.Resources.null;
  protected override System.Drawing.Bitmap Icon => null;

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Base mesh.", GH_ParamAccess.item);
    pManager.AddIntegerParameter("Order",
                                 "K",
                                 "Order of harmonic coordinates (1: harmonic, 2: biharmonic).",
                                 GH_ParamAccess.item,
                                 1);
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddPointParameter(
        "UV Coordinates", "UV", "UV coordinates as 3D points (Z=0).", GH_ParamAccess.list);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    int k = 1;
    if (!DA.GetData(1, ref k)) {
      return;
    }

    // call the cpp function to compute harmonic parametrization
    var uvCoordinates = MeshUtils.GetHarmonicParametrization(ref mesh, k);

    // output
    DA.SetDataList(0, uvCoordinates);
  }
}
}  // namespace igm



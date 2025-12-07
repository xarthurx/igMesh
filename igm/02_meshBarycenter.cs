using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;

namespace igm {
public class IGM_barycenter : GH_Component {
  public IGM_barycenter()
      : base("Barycenter",
             "igBarycenter",
             "compute the barycenter of each triangle of the given mesh.",
             "igMesh",
             "02::Properties") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshBarycenter;
  public override Guid ComponentGuid => new Guid("532f1fab-c27e-4795-99d4-efb55cabc1d4");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddPointParameter(
        "Barycenters", "BC", "The barycenters of the input mesh's faces.", GH_ParamAccess.list);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    // call the cpp function to solve the adjacency list
    var res = MeshUtils.GetBarycenter(ref mesh);

    // output
    DA.SetDataList(0, res);
  }
}
}



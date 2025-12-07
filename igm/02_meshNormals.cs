using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace igm {
public class IGM_normals_vert : GH_Component {
  public IGM_normals_vert()
      : base("Vertex Normal",
             "igNormals_V",
             "Compute per-vertex normals of the given mesh.",
             "igMesh",
             "02::Properties") {}

  public override GH_Exposure Exposure => GH_Exposure.secondary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshNormalVertex;
  public override Guid ComponentGuid => new Guid("51a9a99e-207d-4cb7-b49d-f89bab1b446a");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddVectorParameter("Vertex Normals",
                                "VN",
                                "the per-vertex normals of the input mesh's faces",
                                GH_ParamAccess.list);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    var vn = MeshUtils.GetNormalVert(ref mesh);
    DA.SetDataList(0, vn);
  }
}

public class IGM_normals_face : GH_Component {
  public IGM_normals_face()
      : base("Face Normal",
             "igNormals_F",
             "Compute per-face normals of the given mesh.",
             "igMesh",
             "02::Properties") {}

  public override GH_Exposure Exposure => GH_Exposure.secondary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshNormalFace;
  public override Guid ComponentGuid => new Guid("3b03e8b2-ffd3-4679-b94e-3582f485a877");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddVectorParameter("Face Normals",
                                "FN",
                                "the per-face normals of the input mesh's faces",
                                GH_ParamAccess.list);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    var fn = MeshUtils.GetNormalFace(ref mesh);
    DA.SetDataList(0, fn);
  }
}

public class IGL_normals_edge : GH_Component {
  public IGL_normals_edge()
      : base("Edge Normal",
             "iNormals_E",
             "Compute per-edge normals for a triangle mesh by weighted face normals based on " +
                 "different weighting schemes.",
             "igMesh",
             "02::Properties") {}

  public override GH_Exposure Exposure => GH_Exposure.secondary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshNormalEdge;
  public override Guid ComponentGuid => new Guid("362d6e4a-81ce-4613-8e71-62019f8aff3d");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh to analysis.", GH_ParamAccess.item);
    pManager.AddIntegerParameter(
        "Weighting Type",
        "w",
        "The weighting type how face normals influence edge normals: 0-uniform; 1-area average.",
        GH_ParamAccess.item,
        1);
    pManager[1].Optional = true;
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddVectorParameter(
        "Edge Normal", "EN", "The per edge normals of the input mesh.", GH_ParamAccess.list);
    pManager.AddIntegerParameter("Edge End Index into Vertex List",
                                 "EI",
                                 "The end point indices from the vertex list.",
                                 GH_ParamAccess.tree);
    pManager.AddIntegerParameter("Edge Index",
                                 "EMAP",
                                 "The edge indices from the global edge list (F x 3) to E.",
                                 GH_ParamAccess.list);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    int w = 1;
    if (!DA.GetData(1, ref w)) {}
    if (!(w == 0 || w == 1)) {
      this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "weighting type incorrect.");
      return;
    }

    var (en, ei, emap) = MeshUtils.GetNormalEdge(ref mesh, w);

    Grasshopper.DataTree<int> eiTree = new Grasshopper.DataTree<int>();
    for (int i = 0; i < ei.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      eiTree.AddRange(ei[i], path);
    }

    DA.SetDataList(0, en);
    DA.SetDataTree(1, eiTree);
    DA.SetDataList(2, emap);
  }
}

public class IGM_normals_corner : GH_Component {
  public IGM_normals_corner()
      : base("Corner Normal",
             "igNormals_C",
             "Compute per-corner normals for a triangle mesh by computing the area-weighted " +
                 "average of normals at incident faces whose normals deviate  less than the " +
                 "provided " + "threshold.",
             "igMesh",
             "02::Properties") {}

  public override GH_Exposure Exposure => GH_Exposure.secondary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshNormalCorner;
  public override Guid ComponentGuid => new Guid("a393245d-7c44-4437-b808-8c375e5e6ec4");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddNumberParameter("Degree Threshold",
                                "t",
                                "A threshold in degrees on sharp angles.",
                                GH_ParamAccess.item,
                                10.0);
    pManager[1].Optional = true;
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddVectorParameter("Corner Normals",
                                "CN",
                                "The per-corner normals of the input mesh's faces.",
                                GH_ParamAccess.tree);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
    if (!DA.GetData(0, ref mesh)) {
      return;
    }
    if (!mesh.IsValid) {
      return;
    }

    double t = 10;
    if (!DA.GetData(1, ref t)) {}

    var cn = MeshUtils.GetNormalCorner(ref mesh, t);

    Grasshopper.DataTree<Vector3d> cnTree = new Grasshopper.DataTree<Vector3d>();
    for (int i = 0; i < cn.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      cnTree.AddRange(cn[i], path);
    }

    DA.SetDataTree(0, cnTree);
  }
}
}



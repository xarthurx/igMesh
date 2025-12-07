using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace igm {
public class IGM_vert_vert_adjacency : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_vert_vert_adjacency()
      : base("Vertex-Vertex Adjacency",
             "iAdjVV",
             "Compute the vertex-vertex adjacency relationship of the given mesh.",
             "igMesh",
             "03::Adjacency+Bound") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshAdjacencyVertVert;
  public override Guid ComponentGuid => new Guid("911ef079-7033-43b6-ad57-1d385c5b8406");

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddIntegerParameter(
        "Adjacency Idices", "VV", "the adjacency list of the input mesh", GH_ParamAccess.tree);
    pManager.AddPointParameter(
        "Adjacency Vertices", "P", "the adjacency vertices of the input mesh", GH_ParamAccess.tree);
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

    var res = MeshUtils.GetAdjacencyVV(ref mesh);

    // construct the index & pt tree from the adjacency list
    Grasshopper.DataTree<int> treeArray = new Grasshopper.DataTree<int>();
    Grasshopper.DataTree<Point3d> ptArray = new Grasshopper.DataTree<Point3d>();
    for (int i = 0; i < res.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      treeArray.AddRange(res[i], path);

      foreach (var id in res[i]) {
        ptArray.Add(mesh.Vertices[id], path);
      }
    }

    // assign to the output
    DA.SetDataTree(0, treeArray);
    DA.SetDataTree(1, ptArray);
  }
}

public class IGM_vert_tri_adjacency : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_vert_tri_adjacency()
      : base("Vertex-Triangle Adjacency",
             "igAdjVT",
             "Compute the vertex-triangle adjacency relationship of the given mesh.",
             "igMesh",
             "03::Adjacency+Bound") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshAdjacencyVertTri;
  public override Guid ComponentGuid => new Guid("32c63b12-c43f-40fb-913c-9df607e43305");

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddIntegerParameter(
        "Adjacency V-T",
        "VT",
        "indices of the the ajacent triangles to the corresponding vertex.",
        GH_ParamAccess.tree);
    pManager.AddIntegerParameter("Adjacency V-T",
                                 "VTI",
                                 "index of incidence within incident faces listed in VF.",
                                 GH_ParamAccess.tree);
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

    // call the cpp function to solve the adjacency list
    var (vt, vi) = MeshUtils.GetAdjacencyVT(ref mesh);

    Grasshopper.DataTree<int> adjVT = new Grasshopper.DataTree<int>();
    Grasshopper.DataTree<int> adjVTI = new Grasshopper.DataTree<int>();
    for (int i = 0; i < vt.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      adjVT.AddRange(vt[i], path);
      adjVTI.AddRange(vi[i], path);
    }

    DA.SetDataTree(0, adjVT);
    DA.SetDataTree(1, adjVTI);
  }
}

public class IGM_tri_tri_adjacency : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_tri_tri_adjacency()
      : base("Triangle-Triangle Adjacency",
             "igAdjTT",
             "Compute the triangle-triangle adjacency relationship of the given mesh.",
             "igMesh",
             "03::Adjacency+Bound") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshAdjacencyTriTri;
  public override Guid ComponentGuid => new Guid("f2947234-060d-44de-bd8f-f9601b0e780f");

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddIntegerParameter(
        "Adjacency T-T index",
        "TT",
        "The item {{i}}(j) is the id of the triangle adjacent to the j edge of triangle i.",
        GH_ParamAccess.tree);
    pManager.AddIntegerParameter("Adjacency T-T edge index",
                                 "TTI",
                                 "The item{{i}}(j) is the id of edge of the triangle TT(i,j) " +
                                     "that is adjacent with triangle i.",
                                 GH_ParamAccess.tree);
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

    // call the cpp function to solve the adjacency list
    var (tt, tti) = MeshUtils.GetAdjacencyTT(ref mesh);

    Grasshopper.DataTree<int> adjTT = new Grasshopper.DataTree<int>();
    Grasshopper.DataTree<int> adjTTI = new Grasshopper.DataTree<int>();
    for (int i = 0; i < tt.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      adjTT.AddRange(tt[i], path);
      adjTTI.AddRange(tti[i], path);
    }

    DA.SetDataTree(0, adjTT);
    DA.SetDataTree(1, adjTTI);
  }
}
}

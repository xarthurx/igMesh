using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;
using System.IO;

namespace igm {
public class IGM_mesh_info : GH_Component {
  public IGM_mesh_info()
      : base("Mesh Info",
             "igMeshInfo",
             "Provide various mesh info: V, F, centroid, volume.",
             "igMesh",
             "01::Info+IO") {}
  public override GH_Exposure Exposure => GH_Exposure.secondary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.meshInfo;
  public override Guid ComponentGuid => new Guid("80bb92aa-6cc4-4cd0-bfa8-a841ecbb9da8");

  protected override void RegisterInputParams(GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
    pManager.AddPointParameter(
        "Vertex", "V", "The vertices of the input mesh.", GH_ParamAccess.list);
    pManager.AddIntegerParameter(
        "Face", "F", "The face of the input mesh as three indices into V.", GH_ParamAccess.tree);
    pManager.AddPointParameter("Centroid",
                               "cen",
                               "The centroid using surface integral if the input mesh is closed.",
                               GH_ParamAccess.item);
    pManager.AddNumberParameter("Volume",
                                "vol",
                                "The volume of the mesh if the input mesh is closed.",
                                GH_ParamAccess.item);
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
    var (V, F, cen, vol) = MeshUtils.GetMeshInfo(ref mesh);

    Grasshopper.DataTree<int> fTree = new Grasshopper.DataTree<int>();
    for (int i = 0; i < F.Count; i++) {
      var path = new Grasshopper.Kernel.Data.GH_Path(i);
      fTree.AddRange(F[i], path);
    }

    // output
    DA.SetDataList(0, V);
    DA.SetDataTree(1, fTree);
    DA.SetData(2, cen);
    DA.SetData(3, vol);
    DA.SetData(3, 10);
  }
}

public class igmInfoExporter : GH_Component {
  public igmInfoExporter()
      : base("igMesh Libarary Info",
             "igLibInfo",
             "Check IGM version and whether everything is loaded correctly on all platforms.",
             "igMesh",
             "00::info") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.pluginIcon;
  public override Guid ComponentGuid => new Guid("97a612f9-3fb9-4eb4-a4ed-ec988ac8dab6");

  protected override void RegisterInputParams(GH_InputParamManager pManager) {}

  protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
    pManager.AddTextParameter(
        "Info", "info", "Plugin info for loading native lib.", GH_ParamAccess.item);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    try {
      // Check if the native library is loaded before proceeding
      if (!igMeshBridge.IsNativeLibraryLoaded) {
        string[] errors = igMeshBridge.GetErrorMessages();
        string errorMsg = string.Join("\n", errors);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                          $"Native library not loaded. Check the following errors:\n{errorMsg}");
        return;
      } else {
        // Create an instance of BeingAliveLanguageInfo to access the non-static property
        string version = new igmInfo().AssemblyVersion;
        string loadedPath = igMeshBridge.LoadedLibraryPath;
        string libName = Path.GetFileName(loadedPath);

        string infoText = $"Native library loaded successfully from: {loadedPath}\n" +
                          $"Library name: {libName}\n" + $"Version: {version}";

        DA.SetData(0, infoText);
      }

    } catch (Exception ex) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Exception: {ex.Message}");
    }
  }
}
}


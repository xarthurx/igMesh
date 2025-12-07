using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace igm {
public class IGM_read_triangle_mesh : GH_Component {
  public IGM_read_triangle_mesh()
      : base("Load TriMesh",
             "igLoadMesh",
             "Load a triangle mesh directly from disk. Format supported: obj, off, stl, wrl, " +
                 "ply, mesh.",
             "igMesh",
             "01::Info+IO") {}
  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.ioReadTriMesh;
  public override Guid ComponentGuid => new Guid("0b92e0c5-64a1-4edb-97dc-c8df9f9b088c");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddTextParameter(
        "File Location", "file", "Mesh file location from disk.", GH_ParamAccess.item);
  }
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddMeshParameter(
        "Mesh", "M", "Mesh geometry if loaded successfully.", GH_ParamAccess.item);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    string fName = "";
    if (!DA.GetData(0, ref fName)) {
      return;
    }

    var success = MeshUtils.LoadMesh(fName, out Mesh mesh);

    if (!mesh.IsValid || !success) {
      return;
    }
    // output
    DA.SetData("Mesh", mesh);
  }
}

public class IGM_write_triangle_mesh : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_write_triangle_mesh()
      : base("Save TriMesh",
             "igSaveMesh",
             "Save a triangle mesh directly to disk. format supported: obj, off, stl, wrl, ply, " +
                 "mesh.",
             "igMesh",
             "01::Info+IO") {}

  public override GH_Exposure Exposure => GH_Exposure.primary;
  protected override System.Drawing.Bitmap Icon => Properties.Resources.ioWriteTriMesh;
  public override Guid ComponentGuid => new Guid("fcaceca2-c358-4fc0-8b48-f3d31e861bca");

  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Mesh to save.", GH_ParamAccess.item);
    pManager.AddTextParameter(
        "File Path", "path", "File path to save the mesh to disk.", GH_ParamAccess.item);
    pManager.AddBooleanParameter("Try Save", "T", "Save when TRUE.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddTextParameter(
        "Success", "S", "If mesh is saved successfully.", GH_ParamAccess.item);
  }

  protected override void SolveInstance(IGH_DataAccess DA) {
    Mesh mesh = new Mesh();
    if (!DA.GetData("Mesh", ref mesh) || !mesh.IsValid) {
      return;
    }

    string fName = "";
    if (!DA.GetData("File Path", ref fName)) {
      return;
    }

    bool doIt = false;
    if (!DA.GetData("Try Save", ref doIt)) {
      return;
    }

    if (doIt) {
      var success = MeshUtils.SaveMesh(ref mesh, fName);
      DA.SetData("Success", success ? "Success." : "Failed.");
    }
  }
}
}

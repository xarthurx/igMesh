using GSP;
using GSP.Adapters.Rhino;
using igMesh.Native;
using Grasshopper.Kernel;
using System;

namespace igm {
public class IGM_quad_planarity : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_quad_planarity()
      : base("Quad Mesh Planarity",
             "igQuadPlanarity",
             "Compute the planarity of the quad faces in a quad mesh.",
             "igMesh",
             "09::Utils") {}

  /// <summary>
  /// icon position in a category
  /// </summary>
  public override GH_Exposure Exposure => GH_Exposure.quarternary;

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
    pManager.AddNumberParameter(
        "Face Planarity", "FP", "The planarity of the quad faces.", GH_ParamAccess.list);
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
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input mesh is not valid.");
      return;
    }

    // Validate that the mesh is a pure quad mesh
    bool hasTriangles = false;
    bool hasQuads = false;

    foreach (var face in mesh.Faces) {
      if (face.IsTriangle) {
        hasTriangles = true;
      } else {
        hasQuads = true;
      }

      // Early exit if we detect mixed mesh
      if (hasTriangles && hasQuads) {
        break;
      }
    }

    // Check if mesh is not pure quad
    if (hasTriangles) {
      AddRuntimeMessage(
          GH_RuntimeMessageLevel.Error,
          "Input mesh contains only triangles. This component requires a pure quad mesh.");
      return;
    }

    if (!hasQuads) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input mesh has no faces.");
      return;
    }

    // call the cpp function to solve the adjacency list
    var P = MeshUtils.GetQuadPlanarity(ref mesh);

    // output
    DA.SetDataList(0, P);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      //  return Resources.IconForThisComponent;
      return Properties.Resources.meshPlanarity;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("cc0fea73-0311-4a62-a383-a9423a0b314c"); }
  }
}

public class IGM_quad_planarize : GH_Component {
  /// <summary>
  /// Initializes a new instance of the MyComponent1 class.
  /// </summary>
  public IGM_quad_planarize()
      : base("Quad Mesh Planarize",
             "igQuadPlanarize",
             "Planarize the quad faces in a quad mesh.",
             "igMesh",
             "09::Utils") {}

  /// <summary>
  /// icon position in a category
  /// </summary>
  public override GH_Exposure Exposure => GH_Exposure.quarternary;

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "Input mesh for analysis.", GH_ParamAccess.item);
    pManager.AddIntegerParameter(
        "Iter", "I", "Max iteration for planarization.", GH_ParamAccess.item, 100);
    pManager.AddNumberParameter(
        "Thres", "T", "Threshould to stop the planarization.", GH_ParamAccess.item, 0.005);

    pManager[1].Optional = true;
    pManager[2].Optional = true;
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
    pManager.AddMeshParameter("Mesh", "M", "THe planarized quad mesh.", GH_ParamAccess.item);
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
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input mesh is not valid.");
      return;
    }

    int maxIter = 100;
    if (!DA.GetData(1, ref maxIter) || maxIter < 0) {
      maxIter = 100;
    }
    double thres = 0.005;
    if (!DA.GetData(2, ref thres) || thres <= 0) {
      thres = 0.005;
    }

    // Validate that the mesh is a pure quad mesh
    bool hasTriangles = false;
    bool hasQuads = false;

    foreach (var face in mesh.Faces) {
      if (face.IsTriangle) {
        hasTriangles = true;
      } else {
        hasQuads = true;
      }

      // Early exit if we detect mixed mesh
      if (hasTriangles && hasQuads) {
        break;
      }
    }

    // Check if mesh is not pure quad
    if (hasTriangles) {
      AddRuntimeMessage(
          GH_RuntimeMessageLevel.Error,
          "Input mesh contains triangle faces. This component requires a pure quad mesh. " +
              "Quad planarization only works on quadrilateral faces.");
      return;
    }

    if (!hasQuads) {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input mesh has no faces.");
      return;
    }

    // call the cpp function to solve the adjacency list
    var oMesh = MeshUtils.PlanarizeQuadMesh(ref mesh, maxIter, thres);

    // output
    DA.SetData(0, oMesh);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override System.Drawing.Bitmap Icon {
    get {
      // You can add image files to your project resources and access them like this:
      //  return Resources.IconForThisComponent;
      return Properties.Resources.meshPlanarize;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid {
    get { return new Guid("923f1d6e-e4b0-46e2-b913-e78545bfd7ab"); }
  }
}
}



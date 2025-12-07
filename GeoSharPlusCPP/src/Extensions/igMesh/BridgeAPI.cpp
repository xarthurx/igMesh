#include "GeoSharPlusCPP/Extensions/igMesh/BridgeAPI.h"

#include <algorithm>
#include <iostream>
#include <memory>
#include <ranges>
#include <unordered_map>

#define _USE_MATH_DEFINES
#include <cmath>

#include <igl/adjacency_list.h>
#include <igl/average_onto_faces.h>
#include <igl/average_onto_vertices.h>
#include <igl/avg_edge_length.h>
#include <igl/barycenter.h>
#include <igl/blue_noise.h>
#include <igl/boundary_facets.h>
#include <igl/boundary_loop.h>
#include <igl/centroid.h>
#include <igl/doublearea.h>
#include <igl/fast_winding_number.h>
#include <igl/gaussian_curvature.h>
#include <igl/harmonic.h>
#include <igl/heat_geodesics.h>
#include <igl/map_vertices_to_circle.h>
#include <igl/per_corner_normals.h>
#include <igl/per_edge_normals.h>
#include <igl/per_face_normals.h>
#include <igl/per_vertex_normals.h>
#include <igl/planarize_quad_mesh.h>
#include <igl/principal_curvature.h>
#include <igl/quad_planarity.h>
#include <igl/random_points_on_mesh.h>
#include <igl/read_triangle_mesh.h>
#include <igl/signed_distance.h>
#include <igl/triangle_triangle_adjacency.h>
#include <igl/vertex_triangle_adjacency.h>
#include <igl/write_triangle_mesh.h>

#include "GSP_FB/cpp/intNestedArray_generated.h"
#include "GSP_FB/cpp/mesh_generated.h"
#include "GSP_FB/cpp/pointArray_generated.h"
#include "GSP_FB/cpp/point_generated.h"
#include "GeoSharPlusCPP/Core/MathTypes.h"
#include "GeoSharPlusCPP/Serialization/Serializer.h"

namespace GS = GeoSharPlusCPP::Serialization;

// Helper functions for mesh type handling
namespace {
// Check if mesh requires triangulation for triangle-only functions
[[nodiscard]] constexpr bool requiresTriangulation(const GeoSharPlusCPP::Mesh& mesh) noexcept {
  return mesh.F.cols() == 4;  // Quad mesh needs triangulation
}

// Triangulate a quad mesh by splitting each quad into 2 triangles
[[nodiscard]] GeoSharPlusCPP::Mesh triangulate(const GeoSharPlusCPP::Mesh& mesh) {
  if (mesh.F.cols() == 3) {
    return mesh;  // Already triangulated
  }

  GeoSharPlusCPP::Mesh triMesh;
  triMesh.V = mesh.V;

  // Convert quads to triangles (split each quad into 2 triangles)
  triMesh.F.resize(mesh.F.rows() * 2, 3);
  for (int i = 0; i < mesh.F.rows(); ++i) {
    // Triangle 1: vertices 0,1,2
    triMesh.F(i * 2, 0) = mesh.F(i, 0);
    triMesh.F(i * 2, 1) = mesh.F(i, 1);
    triMesh.F(i * 2, 2) = mesh.F(i, 2);

    // Triangle 2: vertices 0,2,3
    triMesh.F(i * 2 + 1, 0) = mesh.F(i, 0);
    triMesh.F(i * 2 + 1, 1) = mesh.F(i, 2);
    triMesh.F(i * 2 + 1, 2) = mesh.F(i, 3);
  }

  return triMesh;
}

// C++20: Custom deleter for buffer cleanup
struct BufferDeleter {
  void operator()(uint8_t* ptr) const noexcept {
    if (ptr) {
      delete[] ptr;
    }
  }
};

// C++20: RAII wrapper for output buffers
class OutputBuffer {
public:
  OutputBuffer() = default;

  [[nodiscard]] uint8_t** data() noexcept {
    return &buffer_;
  }
  [[nodiscard]] int* size() noexcept {
    return &size_;
  }

  void release(uint8_t** outBuffer, int* outSize) noexcept {
    *outBuffer = buffer_;
    *outSize = size_;
    buffer_ = nullptr;
    size_ = 0;
  }

  ~OutputBuffer() {
    if (buffer_) {
      delete[] buffer_;
    }
  }

private:
  uint8_t* buffer_ = nullptr;
  int size_ = 0;
};

}  // namespace

extern "C" {

GSP_API bool GSP_CALL point3d_roundtrip(const uint8_t* inBuffer,
                                        int inSize,
                                        uint8_t** outBuffer,
                                        int* outSize) {
  *outBuffer = nullptr;
  *outSize = 0;

  GeoSharPlusCPP::Vector3d pt;
  if (!GS::deserializePoint(inBuffer, inSize, pt)) {
    return false;
  }

  // Serialize the point into the allocated buffer
  if (!GS::serializePoint(pt, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL point3d_array_roundtrip(const uint8_t* inBuffer,
                                              int inSize,
                                              uint8_t** outBuffer,
                                              int* outSize) {
  *outBuffer = nullptr;
  *outSize = 0;

  std::vector<GeoSharPlusCPP::Vector3d> points;
  if (!GS::deserializePointArray(inBuffer, inSize, points)) {
    return false;
  }

  // Serialize the point array into the allocated buffer
  if (!GS::serializePointArray(points, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL mesh_roundtrip(const uint8_t* inBuffer,
                                     int inSize,
                                     uint8_t** outBuffer,
                                     int* outSize) {
  *outBuffer = nullptr;
  *outSize = 0;

  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Serialize the mesh into the allocated buffer
  if (!GS::serializeMesh(mesh, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_read_triangle_mesh(const char* filename,
                                             uint8_t** outBuffer,
                                             int* outSize) {
  Eigen::MatrixXd matV;
  Eigen::MatrixXi matF;
  igl::read_triangle_mesh(filename, matV, matF);

  auto mesh = GeoSharPlusCPP::Mesh(matV, matF);

  // Serialize the mesh into the allocated buffer
  if (!GS::serializeMesh(mesh, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_write_triangle_mesh(const uint8_t* inBuffer,
                                              const int inSize,
                                              const char* filename) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  if (!igl::write_triangle_mesh(filename, mesh.V, mesh.F)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_centroid(const uint8_t* inBuffer,
                                   int inSize,
                                   uint8_t** outBuffer,
                                   int* outSize) {
  *outBuffer = nullptr;
  *outSize = 0;
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::Vector3d cen;
  igl::centroid(mesh.V, mesh.F, cen);

  // Serialize the centroid into the allocated buffer
  if (!GS::serializePoint(cen, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;
    return false;
  }
  return true;
}
GSP_API bool GSP_CALL IGM_barycenter(const uint8_t* inBuffer,
                                     int inSize,
                                     uint8_t** outBuffer,
                                     int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXd BC;
  igl::barycenter(mesh.V, mesh.F, BC);

  // Serialize the point array into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  if (!GS::serializePointArray(BC, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_vert_normals(const uint8_t* inBuffer,
                                       int inSize,
                                       uint8_t** outBuffer,
                                       int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXd VN;
  igl::per_vertex_normals(mesh.V, mesh.F, VN);

  // Serialize the point array into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  // Using PointArray serialization for normals
  if (!GS::serializePointArray(VN, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_face_normals(const uint8_t* inBuffer,
                                       int inSize,
                                       uint8_t** outBuffer,
                                       int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXd FN;
  igl::per_face_normals(mesh.V, mesh.F, FN);

  // Serialize the point array into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  // Using PointArray serialization for normals
  if (!GS::serializePointArray(FN, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_corner_normals(
    const uint8_t* inBuffer, int inSize, double threshold_deg, uint8_t** outBuffer, int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXd CN;
  igl::per_corner_normals(mesh.V, mesh.F, threshold_deg, CN);

  // Serialize the point array into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  // Using PointArray serialization for normals
  if (!GS::serializePointArray(CN, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_edge_normals(const uint8_t* inBuffer,
                                       int inSize,
                                       int weightingType,
                                       uint8_t** outBufferA,
                                       int* outSizeA,
                                       uint8_t** outBufferB,
                                       int* outSizeB,
                                       uint8_t** outBufferC,
                                       int* outSizeC) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Calling igl function to compute edge normals
  Eigen::MatrixXd EN;
  Eigen::Matrix<int, Eigen::Dynamic, 2> EI;
  Eigen::VectorXi EMAP;

  igl::per_edge_normals(
      mesh.V, mesh.F, static_cast<igl::PerEdgeNormalsWeightingType>(weightingType), EN, EI, EMAP);

  // Using PointArray serialization for normals
  *outBufferA = nullptr;
  *outSizeA = 0;
  if (!GS::serializePointArray(EN, *outBufferA, *outSizeA)) {
    if (*outBufferA)
      delete[] *outBufferA;  // Cleanup
    *outBufferA = nullptr;
    *outSizeA = 0;

    return false;
  }

  // Using PairIntArray serialization for normals
  *outBufferB = nullptr;
  *outSizeB = 0;
  if (!GS::serializeNumberPairArray(EI, *outBufferB, *outSizeB)) {
    if (*outBufferB)
      delete[] *outBufferB;  // Cleanup
    *outBufferB = nullptr;
    *outSizeB = 0;

    return false;
  }

  // Using NumberArray serialization for normals
  *outBufferC = nullptr;
  *outSizeC = 0;
  if (!GS::serializeNumberArray(EMAP, *outBufferC, *outSizeC)) {
    if (*outBufferC)
      delete[] *outBufferC;  // Cleanup
    *outBufferC = nullptr;
    *outSizeC = 0;

    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_vert_vert_adjacency(const uint8_t* inBuffer,
                                              int inSize,
                                              uint8_t** outBuffer,
                                              int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  std::vector<std::vector<int>> VV;
  igl::adjacency_list(mesh.F, VV);

  // Serialize the adjacency list into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  if (!GS::serializeNestedIntArray(VV, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_vert_tri_adjacency(const uint8_t* inBuffer,
                                             int inSize,
                                             uint8_t** outBufferVT,
                                             int* outSizeVT,
                                             uint8_t** outBufferVTI,
                                             int* outSizeVTI) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  std::vector<std::vector<int>> VF, VFI;
  igl::vertex_triangle_adjacency(mesh.V, mesh.F, VF, VFI);

  // Serialize the first adjacency list (VT)
  *outBufferVT = nullptr;
  *outSizeVT = 0;
  if (!GS::serializeNestedIntArray(VF, *outBufferVT, *outSizeVT)) {
    return false;
  }

  // Serialize the second adjacency list (VTI)
  *outBufferVTI = nullptr;
  *outSizeVTI = 0;
  if (!GS::serializeNestedIntArray(VFI, *outBufferVTI, *outSizeVTI)) {
    // Cleanup first buffer on failure
    if (*outBufferVT)
      delete[] *outBufferVT;
    *outBufferVT = nullptr;
    *outSizeVT = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_tri_tri_adjacency(const uint8_t* inBuffer,
                                            int inSize,
                                            uint8_t** outBufferTT,
                                            int* outSizeTT,
                                            uint8_t** outBufferTTI,
                                            int* outSizeTTI) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXi TT, TTI;
  igl::triangle_triangle_adjacency(mesh.F, TT, TTI);

  // Convert Eigen matrices to nested vectors
  std::vector<std::vector<int>> TT_nested, TTI_nested;
  for (int i = 0; i < TT.rows(); ++i) {
    std::vector<int> tt_row, tti_row;
    for (int j = 0; j < TT.cols(); ++j) {
      tt_row.push_back(TT(i, j));
      tti_row.push_back(TTI(i, j));
    }
    TT_nested.push_back(tt_row);
    TTI_nested.push_back(tti_row);
  }

  // Serialize both adjacency matrices
  *outBufferTT = nullptr;
  *outSizeTT = 0;
  if (!GS::serializeNestedIntArray(TT_nested, *outBufferTT, *outSizeTT)) {
    return false;
  }

  *outBufferTTI = nullptr;
  *outSizeTTI = 0;
  if (!GS::serializeNestedIntArray(TTI_nested, *outBufferTTI, *outSizeTTI)) {
    // Cleanup first buffer on failure
    if (*outBufferTT)
      delete[] *outBufferTT;
    *outBufferTT = nullptr;
    *outSizeTT = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_boundary_loop(const uint8_t* inBuffer,
                                        int inSize,
                                        uint8_t** outBuffer,
                                        int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  std::vector<std::vector<int>> boundaryLoops;
  igl::boundary_loop(mesh.F, boundaryLoops);

  // Serialize the boundary loops into the allocated buffer
  *outBuffer = nullptr;
  *outSize = 0;

  if (!GS::serializeNestedIntArray(boundaryLoops, *outBuffer, *outSize)) {
    if (*outBuffer)
      delete[] *outBuffer;  // Cleanup
    *outBuffer = nullptr;
    *outSize = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_boundary_facet(const uint8_t* inBuffer,
                                         int inSize,
                                         uint8_t** outBufferEL,
                                         int* outSizeEL,
                                         uint8_t** outBufferTL,
                                         int* outSizeTL) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXi F;  // edge list for triangle mesh
  Eigen::VectorXi J, K;
  igl::boundary_facets(mesh.F, F, J, K);

  // Convert edge list (F) to flat integer vector
  std::vector<int> edgeList;
  for (int i = 0; i < F.rows(); ++i) {
    edgeList.push_back(F(i, 0));
    edgeList.push_back(F(i, 1));
  }

  // Serialize the edge list
  *outBufferEL = nullptr;
  *outSizeEL = 0;
  if (!GS::serializeNumberArray(edgeList, *outBufferEL, *outSizeEL)) {
    return false;
  }

  // Serialize the triangle list
  *outBufferTL = nullptr;
  *outSizeTL = 0;
  if (!GS::serializeNumberArray(J, *outBufferTL, *outSizeTL)) {
    // Cleanup first buffer on failure
    if (*outBufferEL)
      delete[] *outBufferEL;
    *outBufferEL = nullptr;
    *outSizeEL = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_remap_VtoF(const uint8_t* inBufferMesh,
                                     int inSizeMesh,
                                     const uint8_t* inBufferScalar,
                                     int inSizeScalar,
                                     uint8_t** outBuffer,
                                     int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<double> scalarData;
  if (!GS::deserializeNumberArray(inBufferScalar, inSizeScalar, scalarData)) {
    return false;
  }

  // Convert to Eigen vector
  Eigen::VectorXd vertexScalars = Eigen::Map<Eigen::VectorXd>(scalarData.data(), scalarData.size());

  // Compute face scalars using IGL
  Eigen::VectorXd faceScalars;
  igl::average_onto_faces(mesh.F, vertexScalars, faceScalars);

  // Serialize result
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(faceScalars, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_remap_FtoV(const uint8_t* inBufferMesh,
                                     int inSizeMesh,
                                     const uint8_t* inBufferScalar,
                                     int inSizeScalar,
                                     uint8_t** outBuffer,
                                     int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<double> scalarData;
  if (!GS::deserializeNumberArray(inBufferScalar, inSizeScalar, scalarData)) {
    return false;
  }

  // Convert to Eigen vector
  Eigen::VectorXd faceScalars = Eigen::Map<Eigen::VectorXd>(scalarData.data(), scalarData.size());

  // Compute vertex scalars using IGL
  Eigen::VectorXd vertexScalars;
  igl::average_onto_vertices(mesh.V, mesh.F, faceScalars, vertexScalars);

  // Serialize result
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(vertexScalars, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_principal_curvature(const uint8_t* inBuffer,
                                              int inSize,
                                              uint32_t radius,
                                              uint8_t** outBufferPD1,
                                              int* outSizePD1,
                                              uint8_t** outBufferPD2,
                                              int* outSizePD2,
                                              uint8_t** outBufferPV1,
                                              int* outSizePV1,
                                              uint8_t** outBufferPV2,
                                              int* outSizePV2) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Auto-triangulate if mesh is quad
  if (requiresTriangulation(mesh)) {
    mesh = triangulate(mesh);
  }

  Eigen::MatrixXd PD1, PD2;
  Eigen::VectorXd PV1, PV2;
  igl::principal_curvature(mesh.V, mesh.F, PD1, PD2, PV1, PV2, radius);

  // Serialize PD1
  *outBufferPD1 = nullptr;
  *outSizePD1 = 0;
  if (!GS::serializePointArray(PD1, *outBufferPD1, *outSizePD1)) {
    return false;
  }

  // Serialize PD2
  *outBufferPD2 = nullptr;
  *outSizePD2 = 0;
  if (!GS::serializePointArray(PD2, *outBufferPD2, *outSizePD2)) {
    if (*outBufferPD1)
      delete[] *outBufferPD1;
    *outBufferPD1 = nullptr;
    *outSizePD1 = 0;
    return false;
  }

  // Serialize PV1
  *outBufferPV1 = nullptr;
  *outSizePV1 = 0;
  if (!GS::serializeNumberArray(PV1, *outBufferPV1, *outSizePV1)) {
    if (*outBufferPD1)
      delete[] *outBufferPD1;
    if (*outBufferPD2)
      delete[] *outBufferPD2;
    *outBufferPD1 = nullptr;
    *outBufferPD2 = nullptr;
    *outSizePD1 = 0;
    *outSizePD2 = 0;
    return false;
  }

  // Serialize PV2
  *outBufferPV2 = nullptr;
  *outSizePV2 = 0;
  if (!GS::serializeNumberArray(PV2, *outBufferPV2, *outSizePV2)) {
    if (*outBufferPD1)
      delete[] *outBufferPD1;
    if (*outBufferPD2)
      delete[] *outBufferPD2;
    if (*outBufferPV1)
      delete[] *outBufferPV1;
    *outBufferPD1 = nullptr;
    *outBufferPD2 = nullptr;
    *outBufferPV1 = nullptr;
    *outSizePD1 = 0;
    *outSizePD2 = 0;
    *outSizePV1 = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_gaussian_curvature(const uint8_t* inBuffer,
                                             int inSize,
                                             uint8_t** outBuffer,
                                             int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Auto-triangulate if mesh is quad
  if (requiresTriangulation(mesh)) {
    mesh = triangulate(mesh);
  }

  Eigen::VectorXd K;
  igl::gaussian_curvature(mesh.V, mesh.F, K);

  // Serialize the curvature values
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(K, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_fast_winding_number(const uint8_t* inBufferMesh,
                                              int inSizeMesh,
                                              const uint8_t* inBufferPoints,
                                              int inSizePoints,
                                              uint8_t** outBuffer,
                                              int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<GeoSharPlusCPP::Vector3d> queryPoints;
  if (!GS::deserializePointArray(inBufferPoints, inSizePoints, queryPoints)) {
    return false;
  }

  // Convert query points to Eigen matrix
  Eigen::MatrixXd Q(queryPoints.size(), 3);
  for (size_t i = 0; i < queryPoints.size(); ++i) {
    Q(i, 0) = queryPoints[i].x();
    Q(i, 1) = queryPoints[i].y();
    Q(i, 2) = queryPoints[i].z();
  }

  Eigen::VectorXd W;
  igl::fast_winding_number(mesh.V, mesh.F, Q, W);

  // Serialize the winding numbers
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(W, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_signed_distance(const uint8_t* inBufferMesh,
                                          int inSizeMesh,
                                          const uint8_t* inBufferPoints,
                                          int inSizePoints,
                                          int signedType,
                                          uint8_t** outBufferSD,
                                          int* outSizeSD,
                                          uint8_t** outBufferFI,
                                          int* outSizeFI,
                                          uint8_t** outBufferCP,
                                          int* outSizeCP) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<GeoSharPlusCPP::Vector3d> queryPoints;
  if (!GS::deserializePointArray(inBufferPoints, inSizePoints, queryPoints)) {
    return false;
  }

  // Convert query points to Eigen matrix
  Eigen::MatrixXd Q(queryPoints.size(), 3);
  for (size_t i = 0; i < queryPoints.size(); ++i) {
    Q(i, 0) = queryPoints[i].x();
    Q(i, 1) = queryPoints[i].y();
    Q(i, 2) = queryPoints[i].z();
  }

  // Ensure signedType is within valid range
  if (signedType < 1 || signedType > 4)
    signedType = 4;

  Eigen::VectorXd S;
  Eigen::VectorXi I;
  Eigen::MatrixXd C;
  Eigen::MatrixXd N;  // temporary variable

  igl::signed_distance(
      Q, mesh.V, mesh.F, static_cast<igl::SignedDistanceType>(signedType), S, I, C, N);

  // Serialize signed distances
  *outBufferSD = nullptr;
  *outSizeSD = 0;
  if (!GS::serializeNumberArray(S, *outBufferSD, *outSizeSD)) {
    return false;
  }

  // Serialize face indices
  *outBufferFI = nullptr;
  *outSizeFI = 0;
  if (!GS::serializeNumberArray(I, *outBufferFI, *outSizeFI)) {
    if (*outBufferSD)
      delete[] *outBufferSD;
    *outBufferSD = nullptr;
    *outSizeSD = 0;
    return false;
  }

  // Serialize closest points
  *outBufferCP = nullptr;
  *outSizeCP = 0;
  if (!GS::serializePointArray(C, *outBufferCP, *outSizeCP)) {
    if (*outBufferSD)
      delete[] *outBufferSD;
    if (*outBufferFI)
      delete[] *outBufferFI;
    *outBufferSD = nullptr;
    *outBufferFI = nullptr;
    *outSizeSD = 0;
    *outSizeFI = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_quad_planarity(const uint8_t* inBuffer,
                                         int inSize,
                                         uint8_t** outBuffer,
                                         int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::VectorXd P;
  igl::quad_planarity(mesh.V, mesh.F, P);

  // Serialize the planarity values
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(P, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_planarize_quad_mesh(const uint8_t* inBuffer,
                                              int inSize,
                                              int maxIter,
                                              double threshold,
                                              uint8_t** outBuffer,
                                              int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Validate that this is actually a quad mesh
  if (mesh.F.cols() != 4) {
    // Not a quad mesh - return error
    return false;
  }

  // Convert RowMajor to ColMajor for libigl compatibility
  // libigl's planarize_quad_mesh requires ColMajor matrices
  Eigen::Matrix<double, Eigen::Dynamic, Eigen::Dynamic, Eigen::ColMajor> V_col(mesh.V.rows(),
                                                                               mesh.V.cols());
  V_col = mesh.V;

  Eigen::Matrix<int, Eigen::Dynamic, Eigen::Dynamic, Eigen::ColMajor> F_col(mesh.F.rows(),
                                                                            mesh.F.cols());
  F_col = mesh.F;

  Eigen::MatrixXd VPlanarized;
  igl::planarize_quad_mesh(V_col, F_col, maxIter, threshold, VPlanarized);

  // Create result mesh with planarized vertices
  GeoSharPlusCPP::Mesh planarizedMesh;
  planarizedMesh.V = VPlanarized;  // Convert ColMajor back to RowMajor via assignment
  planarizedMesh.F = mesh.F;       // Keep original quad topology

  // Serialize the planarized mesh
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeMesh(planarizedMesh, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_laplacian_scalar(const uint8_t* inBufferMesh,
                                           int inSizeMesh,
                                           const uint8_t* inBufferIndices,
                                           int inSizeIndices,
                                           const uint8_t* inBufferValues,
                                           int inSizeValues,
                                           uint8_t** outBuffer,
                                           int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<int> constraintIndices;
  if (!GS::deserializeNumberArray(inBufferIndices, inSizeIndices, constraintIndices)) {
    return false;
  }

  std::vector<double> constraintValues;
  if (!GS::deserializeNumberArray(inBufferValues, inSizeValues, constraintValues)) {
    return false;
  }

  if (constraintIndices.size() != constraintValues.size()) {
    return false;
  }

  // Convert to Eigen types
  Eigen::VectorXi b(constraintIndices.size());
  Eigen::VectorXd bc(constraintValues.size());

  for (size_t i = 0; i < constraintIndices.size(); ++i) {
    b(i) = constraintIndices[i];
    bc(i) = constraintValues[i];
  }

  // Solve harmonic function (Laplacian with constraints)
  Eigen::VectorXd Z;
  igl::harmonic(mesh.V, mesh.F, b, bc, 1, Z);

  // Serialize the result
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(Z, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL
IGM_param_harmonic(const uint8_t* inBuffer, int inSize, int k, uint8_t** outBuffer, int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Find boundary vertices
  Eigen::VectorXi bnd;
  igl::boundary_loop(mesh.F, bnd);

  // Map boundary vertices to circle
  Eigen::MatrixXd bnd_uv;
  igl::map_vertices_to_circle(mesh.V, bnd, bnd_uv);

  // Compute harmonic parametrization
  Eigen::MatrixXd V_uv;
  igl::harmonic(mesh.V, mesh.F, bnd, bnd_uv, k, V_uv);

  // Convert UV coordinates to 3D points (Z = 0)
  Eigen::MatrixXd uvPoints(V_uv.rows(), 3);
  uvPoints.col(0) = V_uv.col(0);  // U coordinate
  uvPoints.col(1) = V_uv.col(1);  // V coordinate
  uvPoints.col(2).setZero();      // Z coordinate = 0

  // Serialize the UV coordinates
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializePointArray(uvPoints, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

// Heat geodesics data structure - we'll use a simple approach with global storage
// In production, you might want a better memory management system
struct HeatGeodesicsPrecomputedData {
  igl::HeatGeodesicsData<double> data;
  bool is_valid;

  HeatGeodesicsPrecomputedData() : is_valid(false) {}
};

// Global storage for precomputed data (simplified approach)
static std::unordered_map<std::size_t, std::unique_ptr<HeatGeodesicsPrecomputedData>>
    heat_geodesics_cache;
static std::size_t next_handle = 1;

GSP_API bool GSP_CALL IGM_heat_geodesic_precompute(const uint8_t* inBuffer,
                                                   int inSize,
                                                   uint8_t** outBuffer,
                                                   int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Create precomputed data structure
  auto precomputed = std::make_unique<HeatGeodesicsPrecomputedData>();

  // Compute average edge length for time parameter
  double t = std::pow(igl::avg_edge_length(mesh.V, mesh.F), 2);

  // Precompute heat geodesics data
  if (!igl::heat_geodesics_precompute(mesh.V, mesh.F, t, precomputed->data)) {
    return false;
  }

  precomputed->is_valid = true;

  // Store in cache and get handle
  std::size_t handle = next_handle++;
  heat_geodesics_cache[handle] = std::move(precomputed);

  // Serialize the handle as a double (for simplicity)
  std::vector<double> handle_vec = {static_cast<double>(handle)};
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(handle_vec, *outBuffer, *outSize)) {
    // Clean up on failure
    heat_geodesics_cache.erase(handle);
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_heat_geodesic_solve(const uint8_t* inBuffer,
                                              int inSize,
                                              const uint8_t* inBufferSources,
                                              int inSizeSources,
                                              uint8_t** outBuffer,
                                              int* outSize) {
  // Deserialize handle as double and convert to size_t
  std::vector<double> handle_vec;
  if (!GS::deserializeNumberArray(inBuffer, inSize, handle_vec) || handle_vec.empty()) {
    return false;
  }

  std::size_t handle = static_cast<std::size_t>(handle_vec[0]);

  // Find precomputed data
  auto it = heat_geodesics_cache.find(handle);
  if (it == heat_geodesics_cache.end() || !it->second->is_valid) {
    return false;
  }

  // Deserialize source vertex indices
  std::vector<int> sources;
  if (!GS::deserializeNumberArray(inBufferSources, inSizeSources, sources)) {
    return false;
  }

  // Convert to Eigen vector
  Eigen::VectorXi gamma = Eigen::Map<Eigen::VectorXi>(sources.data(), sources.size());

  // Solve for geodesic distances
  Eigen::VectorXd distances;
  igl::heat_geodesics_solve(it->second->data, gamma, distances);

  // Serialize the distances
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializeNumberArray(distances, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_random_point_on_mesh(const uint8_t* inBuffer,
                                               int inSize,
                                               int N,
                                               uint8_t** outBufferPoints,
                                               int* outSizePoints,
                                               uint8_t** outBufferFI,
                                               int* outSizeFI) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  Eigen::MatrixXd B, P;
  Eigen::VectorXi FI;

  igl::random_points_on_mesh(N, mesh.V, mesh.F, B, FI, P);

  // Serialize the points
  *outBufferPoints = nullptr;
  *outSizePoints = 0;
  if (!GS::serializePointArray(P, *outBufferPoints, *outSizePoints)) {
    return false;
  }

  // Serialize the face indices
  *outBufferFI = nullptr;
  *outSizeFI = 0;
  if (!GS::serializeNumberArray(FI, *outBufferFI, *outSizeFI)) {
    // Cleanup on failure
    if (*outBufferPoints)
      delete[] *outBufferPoints;
    *outBufferPoints = nullptr;
    *outSizePoints = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_blue_noise_sampling_on_mesh(const uint8_t* inBuffer,
                                                      int inSize,
                                                      int N,
                                                      uint8_t** outBufferPoints,
                                                      int* outSizePoints,
                                                      uint8_t** outBufferFI,
                                                      int* outSizeFI) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBuffer, inSize, mesh)) {
    return false;
  }

  // Compute the radius from desired number using double area
  Eigen::VectorXd A;
  igl::doublearea(mesh.V, mesh.F, A);
  const double r = std::sqrt(((A.sum() * 0.5 / (N * 0.6162910373)) / M_PI));

  Eigen::MatrixXd B, P;
  Eigen::VectorXi FI;

  igl::blue_noise(mesh.V, mesh.F, r, B, FI, P);

  // Serialize the points
  *outBufferPoints = nullptr;
  *outSizePoints = 0;
  if (!GS::serializePointArray(P, *outBufferPoints, *outSizePoints)) {
    return false;
  }

  // Serialize the face indices
  *outBufferFI = nullptr;
  *outSizeFI = 0;
  if (!GS::serializeNumberArray(FI, *outBufferFI, *outSizeFI)) {
    // Cleanup on failure
    if (*outBufferPoints)
      delete[] *outBufferPoints;
    *outBufferPoints = nullptr;
    *outSizePoints = 0;
    return false;
  }

  return true;
}

GSP_API bool GSP_CALL IGM_constrained_scalar(const uint8_t* inBufferMesh,
                                             int inSizeMesh,
                                             const uint8_t* inBufferIndices,
                                             int inSizeIndices,
                                             const uint8_t* inBufferValues,
                                             int inSizeValues,
                                             uint8_t** outBuffer,
                                             int* outSize) {
  // This is essentially the same as IGM_laplacian_scalar, so we can delegate to it
  return IGM_laplacian_scalar(inBufferMesh,
                              inSizeMesh,
                              inBufferIndices,
                              inSizeIndices,
                              inBufferValues,
                              inSizeValues,
                              outBuffer,
                              outSize);
}

GSP_API bool GSP_CALL IGM_extract_isoline_from_scalar(const uint8_t* inBufferMesh,
                                                      int inSizeMesh,
                                                      const uint8_t* inBufferScalar,
                                                      int inSizeScalar,
                                                      const uint8_t* inBufferIsoValues,
                                                      int inSizeIsoValues,
                                                      uint8_t** outBuffer,
                                                      int* outSize) {
  GeoSharPlusCPP::Mesh mesh;
  if (!GS::deserializeMesh(inBufferMesh, inSizeMesh, mesh)) {
    return false;
  }

  std::vector<double> scalarData;
  if (!GS::deserializeNumberArray(inBufferScalar, inSizeScalar, scalarData)) {
    return false;
  }

  std::vector<double> isoValues;
  if (!GS::deserializeNumberArray(inBufferIsoValues, inSizeIsoValues, isoValues)) {
    return false;
  }

  // Convert scalar data to Eigen vector
  Eigen::VectorXd S = Eigen::Map<Eigen::VectorXd>(scalarData.data(), scalarData.size());

  // For now, we'll return a simplified implementation that just serializes all isolevel points
  // This would need a proper isoline extraction algorithm like marching triangles
  // For this implementation, we'll create placeholder points along mesh edges where isolevels occur

  std::vector<GeoSharPlusCPP::Vector3d> allIsolinePoints;

  // Simple edge-based isoline extraction
  for (double isoValue : isoValues) {
    for (int f = 0; f < mesh.F.rows(); ++f) {
      for (int e = 0; e < 3; ++e) {
        int v1 = mesh.F(f, e);
        int v2 = mesh.F(f, (e + 1) % 3);

        double s1 = S(v1);
        double s2 = S(v2);

        // Check if isoline crosses this edge
        if ((s1 <= isoValue && s2 >= isoValue) || (s1 >= isoValue && s2 <= isoValue)) {
          // Interpolate position along edge
          double t = (isoValue - s1) / (s2 - s1);
          if (t >= 0.0 && t <= 1.0) {
            Eigen::Vector3d p1 = mesh.V.row(v1);
            Eigen::Vector3d p2 = mesh.V.row(v2);
            Eigen::Vector3d isoPoint = p1 + t * (p2 - p1);

            allIsolinePoints.emplace_back(isoPoint.x(), isoPoint.y(), isoPoint.z());
          }
        }
      }
    }
  }

  // Serialize all the isoline points
  *outBuffer = nullptr;
  *outSize = 0;
  if (!GS::serializePointArray(allIsolinePoints, *outBuffer, *outSize)) {
    return false;
  }

  return true;
}

}  // extern "C"

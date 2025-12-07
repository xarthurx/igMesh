<!-- title img and links -->

![](https://raw.githubusercontent.com/xarthurx/igMesh/refs/heads/main/graphics/title_img.jpg)

<h1 align="center">igMesh</h1>
<div align="center">
	<a href="https://github.com/xarthurx/igMesh/">Home</a>
  <span> • </span>
	<a href="https://github.com/xarthurx/igMesh/#Installation">Install</a>
  <span> • </span>
       	<a href="https://github.com/xarthurx/igMesh/#Contribution">Contribute</a>
  <p></p>
</div>

<!-- shields.io stuff -->
<div align="center">

<a href="https://www.rhino3d.com/" >
<img alt="License" src="https://img.shields.io/badge/Rhino-8.0-9cf?style=flat-square"> </a>

<a href="https://www.grasshopper3d.com" >
<img alt="License" src="https://img.shields.io/badge/Grasshopper-1.0-brightgreen?style=flat-square"> </a>

<a href="https://github.com/xarthurx/igMesh/blob/master/LICENSE" >
<img alt="License" src="https://img.shields.io/github/license/xarthurx/igMesh?style=flat-square"> </a>

</div>

## A mesh processing library for Grasshopper & Rhino

[**igMesh**](https://github.com/xarthurx/igMesh/) (standing for "I Get a Mesh..." or "Igl-Grasshopper Mesh") is a grasshopper plugin for both low-level and advanced mesh processing.

The library features tools for extracting both low-level information (e.g. vertex, edge, face relationship) and high-level properties (e.g. isolines, quad-planarization) that many mesh processing plugins do not provide.

**igMesh** is developed for the general architecture, design, and fabrication community, hoping to fill the gap of mesh operations in any design-to-fabrication pipelines. The goal is to resolve the long-lasting pain of interactive mesh processing on the [Rhino](https://www.rhino3d.com) \& [Grasshopper](https://www.grasshopper3d.com) platform.

### Components Overview

![](https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/overview_img.png)

### Showcase

<p align="center">
  <img src="https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/demo.gif" alt="demo.gif"/>
</p>

### Highlighted Components

#### <u>universal random points (blue noise)</u>:

![Universal Random Distribution](https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/showcase_randomPts.png)

<details><summary> More (Click to expand!)... </summary>

#### <u>fast geodesic distance</u>:

![Heat Geodesics](https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/showcase_heatGeodesics.png)

#### <u>mesh isoline</u>:

![Mesh Isoline](https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/showcase_isoline.png)

#### <u>quad-mesh planarization</u>:

![Quad Planarization](https://raw.githubusercontent.com/xarthurx/igMesh/master/graphics/showcase_planarization.png)

</details>

## ALPHA-phase and Use case collection

**This library is currently under the alpha-phase for initial public test. To further improve it, your contribution is needed.**

Please submit an issue and describe what your mesh processing task requires and what type of functions are missing.

I will add the corresponding functions to the library after evaluation, ASAP.

## Installation

### Install with **PackageManager** (Rhino 7+)

1. Open Rhino and run command `PackageManager`.
2. Search for "**igMesh**".
3. Install the plugin and restart Rhino.

### Food4Rhino

The [Project Page](https://www.food4rhino.com/en/app/ig-mesh) on Food4Rhino also holds major release versions of this library.

### Pre-compiled release

Pre-compiled releases are available on the [GitHub repo](https://github.com/xarthurx/igMesh).

1. Download the `.zip` file from [the latest release](https://github.com/xarthurx/igMesh/releases/latest).
2. Unzip the `.zip` file and put the folder into you _Grasshopper Component Folder_.
3. Restart _Rhino_ and _Grasshopper_.

_Releases with minor updates will only be published as pre-compiled releases and be hosted on github._

## Planned Feature

### TODO

Below are the current planned features to be added in the next release:

- ~~planarization using [_Shape-Up_](https://lgg.epfl.ch/publications/2012/shapeup/index.php)~~ (done in v0.2)
- ~~Fast geodesic distance based on the "Heat-kernel" method~~ (done in v0.2)
- Half-Edge structure

### Future Plan (Non-Goal TODO)

Below is an incomplete list of functions that `igMesh` plans to provide. The list is constantly adjusted based on feedback:

- edge-related functions for vector fields operation
- Various approaches for unrolling mesh (parametrization)
- FEM-related functions (need evaluation on speed and computational efficiency)
- voxel (tet-based) processing functionality

## Contribution

You need `Visual Studio 2022` and `CMake` to compile the project.

### Prebuild for CPP Dependencies

Go to the `GeoSharPlusCPP` folder and run `cmake build .` to build the C++ dependencies.

This will generate a `generated` folder containing the required geometry transmission types using `flatbuffer`.

### Build the projec

Open the solution file with `Visual Studio 2022+` and compile the project.

## Acknowledgement and License

Many of the base functions are converted from the geometry processing library [libigl](https://libigl.github.io), and ported into C# environment through the [PInvoke](https://www.grasshopper3d.com/forum/topics/link-use-c-code-or-c-lib-with-new-gh-plugin) methods and [openNURBS](https://github.com/mcneel/opennurbs).

The author would like to pay his deepest gratitude to the developers for this library and the responsive Q\&A during the past years[^1].

[^1]: _The name of this library is also partially inspired by the [libigl](https://libigl.github.io) library._

**The library is released under the [GPL licence](https://github.com/xarthurx/igMesh/blob/master/LICENSE).**

---

## Credit & Citation

Author: [Zhao Ma](https://beyond-disciplines.com)

If `igMesh` contributes to an academic publication, please cite it as:

```bib
@software{ig-mesh,
  title = {IG-Mesh},
  author = {Zhao Ma},
  url = {https://github.com/xarthurx/igMesh},
  doi = {10.5281/zenodo.6499203},
  version = {0.5.3}
  year = {2022}
}
```

You can also find the reference infomation on [Zenodo.org](https://zenodo.org/record/6499203).

cmake_minimum_required(VERSION 3.31)

set(PROJECT_NAME GeoSharPlusCPP)

# Set the Visual Studio toolset to the latest version
if(MSVC)
    set(CMAKE_GENERATOR_TOOLSET "v145" CACHE STRING "Visual Studio toolset version" FORCE)
    message(STATUS "Using Visual Studio 2026 toolset: ${CMAKE_GENERATOR_TOOLSET}")
endif()

# Check for VCPKG_ROOT environment variable
if(DEFINED ENV{VCPKG_ROOT})
    set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")
    message(STATUS "Using vcpkg toolchain file: ${CMAKE_TOOLCHAIN_FILE}")
else()
    message(FATAL_ERROR "VCPKG_ROOT environment variable is not defined. Please set it to your vcpkg installation directory.")
endif()


# Explicitly set vcpkg target triplet if not already set
if(NOT DEFINED VCPKG_TARGET_TRIPLET)
    if(WIN32)
        set(VCPKG_TARGET_TRIPLET "x64-windows" CACHE STRING "vcpkg target triplet")
    elseif(APPLE)
        set(VCPKG_TARGET_TRIPLET "x64-osx" CACHE STRING "vcpkg target triplet")
    elseif(UNIX)
        set(VCPKG_TARGET_TRIPLET "x64-linux" CACHE STRING "vcpkg target triplet")
    endif()
    message(STATUS "Setting vcpkg triplet to: ${VCPKG_TARGET_TRIPLET}")
endif()


# Set C++ standard
project(${PROJECT_NAME} LANGUAGES C CXX)

# Set C++ standard to C++20
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "C++ compiler: ${CMAKE_CXX_COMPILER}")
message(STATUS "C++ standard: C++${CMAKE_CXX_STANDARD}")
message(STATUS "Generator: ${CMAKE_GENERATOR}")

# Find dependencies
find_package(Eigen3 CONFIG REQUIRED)
message(STATUS "Found Eigen3: ${EIGEN3_INCLUDE_DIR}")

find_package(libigl CONFIG REQUIRED)
message(STATUS "Found libigl: ${libigl_DIR}")

find_package(Flatbuffers CONFIG REQUIRED)
message(STATUS "Found Flatbuffers: ${Flatbuffers_DIR}")
if(NOT EXISTS "${FLATBUFFERS_FLATC_EXECUTABLE}")
	find_program(flatbuffers_FLATC_EXECUTABLE flatc
		PATHS "${Flatbuffers_DIR}/tools/flatbuffers"
		PATH_SUFFIXES bin
	)
	if(flatbuffers_FLATC_EXECUTABLE)
		message(STATUS "Found flatc by searching: ${flatbuffers_FLATC_EXECUTABLE}")
	else()
		message(FATAL_ERROR "flatc compiler not found. Please ensure Flatbuffers is properly installed.")
	endif()
endif()
message(STATUS "flatc executable path: ${FLATBUFFERS_FLATC_EXECUTABLE}")

# Print all CMake variables for debugging
message(STATUS "CMAKE_PREFIX_PATH: ${CMAKE_PREFIX_PATH}")
message(STATUS "CMAKE_MODULE_PATH: ${CMAKE_MODULE_PATH}")

###########################################
# FLATBUFFER GENERATION
###########################################
# FlatBuffers code generation setup
file(GLOB FLATBUFFERS_SCHEMAS "${CMAKE_CURRENT_SOURCE_DIR}/schema/*.fbs")
set(FLATBUFFERS_OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../generated/GSP_FB")

# Create output directories
file(MAKE_DIRECTORY "${FLATBUFFERS_OUTPUT_DIR}")
file(MAKE_DIRECTORY "${FLATBUFFERS_OUTPUT_DIR}/cpp")
file(MAKE_DIRECTORY "${FLATBUFFERS_OUTPUT_DIR}/csharp")

# Process each schema file
message(STATUS "Found schema files: ${FLATBUFFERS_SCHEMAS}")
foreach(SCHEMA_FILE ${FLATBUFFERS_SCHEMAS})
    get_filename_component(SCHEMA_NAME ${SCHEMA_FILE} NAME_WE)
    message(STATUS "Processing schema: ${SCHEMA_NAME}")
    
    # Generate C++ code
    message(STATUS "Generating Flatbuffers C++ code for ${SCHEMA_NAME}...")
    execute_process(
        COMMAND ${flatbuffers_FLATC_EXECUTABLE}
        --cpp
        --scoped-enums
        -o "${FLATBUFFERS_OUTPUT_DIR}/cpp"
        "${SCHEMA_FILE}"
        RESULT_VARIABLE FLATC_CPP_RESULT
    )

    if(NOT FLATC_CPP_RESULT EQUAL 0)
        message(FATAL_ERROR "Failed to generate C++ Flatbuffers code for ${SCHEMA_NAME}")
    endif()

    # Generate C# code
    message(STATUS "Generating Flatbuffers C# code for ${SCHEMA_NAME}...")
    execute_process(
        COMMAND ${flatbuffers_FLATC_EXECUTABLE}
        --csharp
        --scoped-enums
        --gen-onefile
        --gen-object-api
        -o "${FLATBUFFERS_OUTPUT_DIR}/csharp"
        "${SCHEMA_FILE}"
        RESULT_VARIABLE FLATC_CSHARP_RESULT
    )

    if(NOT FLATC_CSHARP_RESULT EQUAL 0)
        message(FATAL_ERROR "Failed to generate C# Flatbuffers code for ${SCHEMA_NAME}")
    endif()
endforeach()

message(STATUS "Flatbuffers generation complete")


###########################################
# GEOSHARPLUS TARGET
###########################################
# Add header files to the target
file(GLOB_RECURSE HEADERS CONFIGURE_DEPENDS 
    "${CMAKE_CURRENT_SOURCE_DIR}/include/*.h"
)

file(GLOB_RECURSE SOURCES CONFIGURE_DEPENDS 
    "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp"
)

add_library(${PROJECT_NAME} SHARED ${HEADERS} ${SOURCES})

# Organize VS folders
source_group("Header Files" FILES ${HEADERS})
source_group("Source Files" FILES ${SOURCES})

# Critical include path
target_include_directories(${PROJECT_NAME} PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${CMAKE_CURRENT_SOURCE_DIR}/../generated  # For generated FlatBuffers code
    ${libigl_INCLUDE_DIRS}
)

# Rest of your CMakeLists.txt remains the same
# Dependencies
target_link_libraries(${PROJECT_NAME} PRIVATE
    Eigen3::Eigen
    igl::igl_core
    flatbuffers::flatbuffers
)

# No need to add dependency since we generate at configure time

# Platform-specific configuration
if(WIN32)
    target_compile_definitions(${PROJECT_NAME} PRIVATE GEOSHARPLUS_EXPORTS)
    target_compile_options(${PROJECT_NAME} PRIVATE /W4 /EHsc)
else()
    target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra -pedantic)
endif()

############################################
# Post-build steps
############################################
# Create bin directory if it doesn't exist
# file(MAKE_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../bin")
file(MAKE_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../cppPrebuild")

# Add post-build command to copy the DLL to the bin directory
add_custom_command(
    TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy 
        $<TARGET_FILE:${PROJECT_NAME}> 
        "${CMAKE_CURRENT_SOURCE_DIR}/../cppPrebuild/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    COMMENT "Copying ${PROJECT_NAME} DLL to bin directory"
)

cmake_minimum_required(VERSION 3.16)
project(DeviceInterface)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find packages
find_package(OpenCV REQUIRED)

# Mech-Eye SDK Configuration
set(MECHEYE_SDK_ROOT "C:/Mech-Mind/Mech-Eye SDK-2.5.1/API")
set(MECHEYE_INCLUDE_DIR "${MECHEYE_SDK_ROOT}/include")
set(MECHEYE_LIB_DIR "${MECHEYE_SDK_ROOT}/lib")
set(MECHEYE_LIB_DEBUG_DIR "${MECHEYE_SDK_ROOT}/lib_debug")
set(MECHEYE_DLL_DIR "${MECHEYE_SDK_ROOT}/dll")
set(MECHEYE_DLL_DEBUG_DIR "${MECHEYE_SDK_ROOT}/dll_debug")

# Find Mech-Eye libraries for Release and Debug
find_library(MECHEYE_LIB_RELEASE NAMES MechEyeAPI PATHS ${MECHEYE_LIB_DIR} NO_DEFAULT_PATH)
find_library(MECHEYE_LIB_DEBUG NAMES MechEyeAPI PATHS ${MECHEYE_LIB_DEBUG_DIR} NO_DEFAULT_PATH)

# Create imported target for Mech-Eye SDK (only if release library found)
if(MECHEYE_LIB_RELEASE)
    add_library(MechEyeSDK SHARED IMPORTED)
    
    # Always set release library
    set_target_properties(MechEyeSDK PROPERTIES
        IMPORTED_IMPLIB_RELEASE "${MECHEYE_LIB_RELEASE}"
        INTERFACE_INCLUDE_DIRECTORIES "${MECHEYE_INCLUDE_DIR}"
    )
    
    # Only set debug library if it exists
    if(MECHEYE_LIB_DEBUG)
        set_target_properties(MechEyeSDK PROPERTIES
            IMPORTED_IMPLIB_DEBUG "${MECHEYE_LIB_DEBUG}"
        )
    else()
        # Use release library for debug builds if debug not found
        message(WARNING "Debug library not found, using release library for debug builds")
        set_target_properties(MechEyeSDK PROPERTIES
            IMPORTED_IMPLIB_DEBUG "${MECHEYE_LIB_RELEASE}"
        )
    endif()
else()
    message(WARNING "Mech-Eye SDK library not found. Building without Mech-Eye support.")
endif()

# ============================================================================
# MAIN TARGET: DeviceInterface DLL (now includes LiveStreamProcessor)
# ============================================================================
add_library(DeviceInterface SHARED
    "DeviceInterface.cpp"
    "DeviceInterface.h"
    "VirtualCamera.cpp"
    "VirtualCamera.h"
    "LiveStreamProcessor.cpp"
    "LiveStreamProcessor.h"
)

# IMPORTANT: Set exports for both components
target_compile_definitions(DeviceInterface PRIVATE
    DEVICEINTERFACE_EXPORTS
    LIVESTREAMPROCESSOR_EXPORTS
)

# Link libraries for DLL
target_link_libraries(DeviceInterface PRIVATE 
    ${OpenCV_LIBS}
)

# Add MechEyeSDK if found for DLL
if(TARGET MechEyeSDK)
    target_link_libraries(DeviceInterface PRIVATE MechEyeSDK)
    message(STATUS "Linking DeviceInterface DLL with Mech-Eye SDK")
endif()

# Include directories for DLL
target_include_directories(DeviceInterface PRIVATE 
    ${OpenCV_INCLUDE_DIRS}
    ${MECHEYE_INCLUDE_DIR}
)

# Set output directory for DLL
set_target_properties(DeviceInterface PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)

# Copy required DLLs to output directory
if(WIN32)
    # Copy Mech-Eye DLL for release builds
    if(EXISTS "${MECHEYE_DLL_DIR}/MechEyeAPI.dll")
        add_custom_command(TARGET DeviceInterface POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${MECHEYE_DLL_DIR}/MechEyeAPI.dll"
            "${CMAKE_BINARY_DIR}/bin/"
            COMMENT "Copying MechEyeAPI.dll (Release)"
        )
    endif()
    
    # Copy Mech-Eye DLL for debug builds
    if(EXISTS "${MECHEYE_DLL_DEBUG_DIR}/MechEyeAPI.dll")
        add_custom_command(TARGET DeviceInterface POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${MECHEYE_DLL_DEBUG_DIR}/MechEyeAPI.dll"
            "${CMAKE_BINARY_DIR}/bin/"
            COMMENT "Copying MechEyeAPI.dll (Debug)"
        )
    endif()
endif()

message(STATUS "DeviceInterface DLL build configuration complete")
message(STATUS "  - DeviceInterface: Camera connections & basic operations")
message(STATUS "  - LiveStreamProcessor: Live streaming & image processing")
message(STATUS "  - VirtualCamera: Virtual camera simulation")
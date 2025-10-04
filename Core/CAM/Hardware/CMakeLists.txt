cmake_minimum_required(VERSION 3.16)
project(DeviceInterface)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find packages
find_package(OpenCV REQUIRED)

# Debug build info
message(STATUS "=== DeviceInterface Build Configuration ===")
message(STATUS "CMAKE_BUILD_TYPE: ${CMAKE_BUILD_TYPE}")
message(STATUS "CMAKE_BINARY_DIR: ${CMAKE_BINARY_DIR}")

# Mech-Eye SDK Configuration
set(MECHEYE_SDK_ROOT "C:/Mech-Mind/Mech-Eye SDK-2.5.1/API")
set(MECHEYE_INCLUDE_DIR "${MECHEYE_SDK_ROOT}/include")
set(MECHEYE_LIB_DIR "${MECHEYE_SDK_ROOT}/lib")
set(MECHEYE_LIB_DEBUG_DIR "${MECHEYE_SDK_ROOT}/lib_debug")
set(MECHEYE_DLL_DIR "${MECHEYE_SDK_ROOT}/dll")
set(MECHEYE_DLL_DEBUG_DIR "${MECHEYE_SDK_ROOT}/dll_debug")

# Check SDK existence and list DLLs
message(STATUS "=== Mech-Eye SDK Check ===")
message(STATUS "SDK Root: ${MECHEYE_SDK_ROOT}")

# List all DLLs in release directory
if(EXISTS "${MECHEYE_DLL_DIR}")
    file(GLOB MECHEYE_RELEASE_DLLS "${MECHEYE_DLL_DIR}/*.dll")
    message(STATUS "✅ Release DLL directory exists: ${MECHEYE_DLL_DIR}")
    message(STATUS "Found ${CMAKE_LIST_LENGTH} release DLLs:")
    foreach(dll ${MECHEYE_RELEASE_DLLS})
        get_filename_component(dll_name ${dll} NAME)
        message(STATUS "  - ${dll_name}")
    endforeach()
else()
    message(WARNING "❌ Release DLL directory NOT FOUND: ${MECHEYE_DLL_DIR}")
    set(MECHEYE_RELEASE_DLLS "")
endif()

# List all DLLs in debug directory
if(EXISTS "${MECHEYE_DLL_DEBUG_DIR}")
    file(GLOB MECHEYE_DEBUG_DLLS "${MECHEYE_DLL_DEBUG_DIR}/*.dll")
    message(STATUS "✅ Debug DLL directory exists: ${MECHEYE_DLL_DEBUG_DIR}")
    message(STATUS "Found debug DLLs:")
    foreach(dll ${MECHEYE_DEBUG_DLLS})
        get_filename_component(dll_name ${dll} NAME)
        message(STATUS "  - ${dll_name}")
    endforeach()
else()
    message(WARNING "❌ Debug DLL directory NOT FOUND: ${MECHEYE_DLL_DEBUG_DIR}")
    set(MECHEYE_DEBUG_DLLS "")
endif()

# Find Mech-Eye libraries for Release and Debug
find_library(MECHEYE_LIB_RELEASE NAMES MechEyeAPI PATHS ${MECHEYE_LIB_DIR} NO_DEFAULT_PATH)
find_library(MECHEYE_LIB_DEBUG NAMES MechEyeAPI MechEyeApid PATHS ${MECHEYE_LIB_DEBUG_DIR} NO_DEFAULT_PATH)

# Create imported target for Mech-Eye SDK
if(MECHEYE_LIB_RELEASE OR MECHEYE_LIB_DEBUG)
    add_library(MechEyeSDK SHARED IMPORTED)
    
    if(MECHEYE_LIB_RELEASE)
        set_target_properties(MechEyeSDK PROPERTIES
            IMPORTED_IMPLIB_RELEASE "${MECHEYE_LIB_RELEASE}"
            INTERFACE_INCLUDE_DIRECTORIES "${MECHEYE_INCLUDE_DIR}"
        )
        message(STATUS "✅ Release lib: ${MECHEYE_LIB_RELEASE}")
    endif()
    
    if(MECHEYE_LIB_DEBUG)
        set_target_properties(MechEyeSDK PROPERTIES
            IMPORTED_IMPLIB_DEBUG "${MECHEYE_LIB_DEBUG}"
        )
        message(STATUS "✅ Debug lib: ${MECHEYE_LIB_DEBUG}")
    elseif(MECHEYE_LIB_RELEASE)
        # Use release library for debug builds if debug not found
        message(WARNING "Debug library not found, using release library for debug builds")
        set_target_properties(MechEyeSDK PROPERTIES
            IMPORTED_IMPLIB_DEBUG "${MECHEYE_LIB_RELEASE}"
        )
    endif()
    
    message(STATUS "✅ MechEyeSDK target created")
else()
    message(WARNING "❌ Mech-Eye SDK library not found. Building without Mech-Eye support.")
    add_compile_definitions(NO_MECHEYE_SDK)
endif()

# ============================================================================
# MAIN TARGET: DeviceInterface DLL
# ============================================================================
add_library(DeviceInterface SHARED
    "DeviceInterface.cpp"
    "DeviceInterface.h"
    "VirtualCamera.cpp"
    "VirtualCamera.h"
)

# Set exports
target_compile_definitions(DeviceInterface PRIVATE
    DEVICEINTERFACE_EXPORTS
)

# Link libraries
target_link_libraries(DeviceInterface PRIVATE 
    ${OpenCV_LIBS}
)

if(TARGET MechEyeSDK)
    target_link_libraries(DeviceInterface PRIVATE MechEyeSDK)
    message(STATUS "✅ Linking DeviceInterface DLL with Mech-Eye SDK")
else()
    message(STATUS "⚠️ Building DeviceInterface DLL without Mech-Eye SDK")
endif()

# Include directories
target_include_directories(DeviceInterface PRIVATE 
    ${OpenCV_INCLUDE_DIRS}
    ${MECHEYE_INCLUDE_DIR}
)

# Set output directory with config-specific paths
set_target_properties(DeviceInterface PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_BINARY_DIR}/bin/Debug"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_BINARY_DIR}/bin/Release"
    ARCHIVE_OUTPUT_DIRECTORY_DEBUG "${CMAKE_BINARY_DIR}/lib/Debug"
    ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_BINARY_DIR}/lib/Release"
)

# ============================================================================
# COPY ALL MECH-EYE SDK DLLs
# ============================================================================
if(WIN32 AND (MECHEYE_RELEASE_DLLS OR MECHEYE_DEBUG_DLLS))
    message(STATUS "=== Setting up Mech-Eye DLL copy commands ===")
    
    # Copy ALL release DLLs for both Debug and Release builds
    if(MECHEYE_RELEASE_DLLS)
        foreach(dll ${MECHEYE_RELEASE_DLLS})
            get_filename_component(dll_name ${dll} NAME)
            add_custom_command(TARGET DeviceInterface POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy_if_different
                "${dll}"
                "$<TARGET_FILE_DIR:DeviceInterface>/"
                COMMENT "📋 Copying ${dll_name} to output directory"
            )
        endforeach()
        message(STATUS "✅ Added copy commands for ${CMAKE_LIST_LENGTH} release DLLs")
    endif()
    
    # Copy ALL debug DLLs (if different from release)
    if(MECHEYE_DEBUG_DLLS)
        foreach(dll ${MECHEYE_DEBUG_DLLS})
            get_filename_component(dll_name ${dll} NAME)
            # Only copy if it's different from release version
            list(FIND MECHEYE_RELEASE_DLLS "${MECHEYE_DLL_DIR}/${dll_name}" found_in_release)
            if(found_in_release EQUAL -1)
                add_custom_command(TARGET DeviceInterface POST_BUILD
                    COMMAND ${CMAKE_COMMAND} -E copy_if_different
                    "${dll}"
                    "$<TARGET_FILE_DIR:DeviceInterface>/"
                    COMMENT "📋 Copying ${dll_name} (Debug version) to output directory"
                )
            endif()
        endforeach()
        message(STATUS "✅ Added copy commands for debug-specific DLLs")
    endif()
    
    message(STATUS "🎯 All Mech-Eye SDK DLLs will be copied to: $<TARGET_FILE_DIR:DeviceInterface>")
    
else()
    if(NOT WIN32)
        message(STATUS "⚠️ Not Windows - skipping DLL copy")
    else()
        message(STATUS "⚠️ No Mech-Eye DLLs found - skipping SDK DLL copy")
    endif()
endif()

message(STATUS "=== DeviceInterface Configuration Complete ===")
message(STATUS "  - DeviceInterface: Camera connections & basic operations")
message(STATUS "  - VirtualCamera: Virtual camera simulation")
message(STATUS "  - Output: ${CMAKE_BINARY_DIR}/bin/$<CONFIG>")
cmake_minimum_required(VERSION 3.16)
project(SimplifiedArchitecture)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find packages
find_package(OpenCV REQUIRED)

# Debug build info
message(STATUS "=== Simplified Architecture Build Configuration ===")
message(STATUS "CMAKE_BUILD_TYPE: ${CMAKE_BUILD_TYPE}")
message(STATUS "CMAKE_BINARY_DIR: ${CMAKE_BINARY_DIR}")

# Device Interface layer has been removed
# Hardware operations are now handled directly through Camera tab or other means
# This CMake file is kept for potential future hardware integration

message(STATUS "=== Simplified Architecture Complete ===")
message(STATUS "  - Device Interface layer removed")
message(STATUS "  - Hardware operations moved to direct implementations") 
message(STATUS "  - CMake configuration simplified")
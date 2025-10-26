# Code-gen-for-robot-arm — OJT FALL 2025 (FPT)

This repository contains our internship project at **Vietdynamics**, focusing on building an integrated system for robot arm control, code generation, and simulation.

---

## Project Goal

The objective of this project is to develop a **complete application system** that allows users to:

- Interact with **Intel RealSense 3D cameras** for data capture and processing.  
- Perform **object detection** and **feature extraction** on captured data.  
- Generate **robot control code** based on both input data and natural-language task descriptions.  
- **Simulate and validate** generated code to verify correctness before deployment.  
- Support **cross-robot code conversion**, allowing the same task logic to be translated to different robot models or SDKs.

---

## Current Progress

### Core Features Checklist
| Feature | Status | Description |
|----------|:------:|-------------|
| 3D camera integration (Intel RealSense) | ✅ | Fully functional device scanning, connection, and data streaming. |
| Object detection and feature extraction | ✅ | Custom classification model added for object feature work. |
| Robot simulation (Gazebo + ROS or self implement) | In Progress | Basic simulation and validation implemented. |
| Code generation module | In Progress | Code synthesis and translation between robot formats under development. |
| UI integration | ✅ | Functional UI with modular tabs for camera, simulation, and validation. |
| Cross-robot compatibility | Planned | Future goal to enable code conversion between robot types. |

---

## 🧩 Changelog

### v1.0.0
- Added UI and DeviceInterface DLL for UI integration.  
- Implemented device scanning — now fully functional.

### v1.5.0
- Updated UI layout and interaction flow.  
- Added virtual sample device for testing without physical hardware.  
- Implemented image/video streaming from DLL to UI.  
- **Known issue:** Poor performance caused by continuous streaming and poor memory handling.

### v1.5.1
- Updated DeviceInterface DLL and App with fully functional camera support.  
- Added **Camera tab** for full Mech-Eye camera control, **Simulate/Validate tab** for Gazebo & ROS simulation.  
- Removed image/video streaming feature for improved stability and performance.

### v1.5.1a (Updated)
- Added Camera tab for full Mech-Eye camera control, Simulate/Validate tab for result simulation with Gazebo and ROS.

### v1.5.1b (Updated)
- Added **custom classification model** for object feature work.

### v1.5.2
- Added **robot simulation and validation features.**  
  Thanks to [RobotArmHelix](https://github.com/Gabryxx7/RobotArmHelix) for integration support.

<p align="center">
  <img width="500" height="500" alt="Simulation preview" src="https://github.com/user-attachments/assets/d5cb3a14-d951-4dd4-9237-8f14d3558e84" />
</p>

---

##  Next Steps
- Complete the code generation and translation module.  
- Integrate language-based task input for auto code synthesis.  
- Enhance simulation accuracy and add error-feedback loop for generated code.

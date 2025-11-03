// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2017 Intel Corporation. All Rights Reserved.

#include <librealsense2/rs.hpp> // Include RealSense Cross Platform API
#include <iostream>
#include <iomanip>

// Simple console-based RealSense capture example
int main(int argc, char * argv[]) try
{
    std::cout << "RealSense Capture Console Application" << std::endl;
    std::cout << "Device Interface removed - simplified architecture" << std::endl;
    std::cout << "======================================" << std::endl;

    // Enable logging
    rs2::log_to_console(RS2_LOG_SEVERITY_ERROR);

    // Declare RealSense pipeline, encapsulating the actual device and sensors
    rs2::pipeline pipe;

    // Configure pipeline to stream color and depth frames
    rs2::config cfg;
    cfg.enable_stream(RS2_STREAM_COLOR, 640, 480, RS2_FORMAT_BGR8, 30);
    cfg.enable_stream(RS2_STREAM_DEPTH, 640, 480, RS2_FORMAT_Z16, 30);

    // Start streaming with configured parameters
    rs2::pipeline_profile profile = pipe.start(cfg);

    std::cout << "RealSense pipeline started successfully" << std::endl;
    std::cout << "Streaming 640x480 @ 30fps (Color + Depth)" << std::endl;
    std::cout << "Press Ctrl+C to stop..." << std::endl;

    int frame_count = 0;
    auto start_time = std::chrono::high_resolution_clock::now();

    while (true) // Capture frames continuously
    {
        try {
            // Wait for next set of frames from the camera
            rs2::frameset frames = pipe.wait_for_frames(5000); // 5 second timeout

            // Get color and depth frames
            rs2::frame color_frame = frames.get_color_frame();
            rs2::frame depth_frame = frames.get_depth_frame();

            frame_count++;

            // Print frame info every 30 frames (roughly once per second)
            if (frame_count % 30 == 0)
            {
                auto current_time = std::chrono::high_resolution_clock::now();
                auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(
                    current_time - start_time).count();

                double fps = (frame_count * 1000.0) / duration;

                std::cout << "Frame " << std::setw(6) << frame_count 
                         << " | FPS: " << std::fixed << std::setprecision(1) << fps
                         << " | Color: " << color_frame.get_width() << "x" << color_frame.get_height()
                         << " | Depth: " << depth_frame.get_width() << "x" << depth_frame.get_height()
                         << std::endl;
            }
        }
        catch (const rs2::error& e) {
            std::cerr << "RealSense error: " << e.what() << std::endl;
            break;
        }
    }

    std::cout << "Stopping pipeline..." << std::endl;
    pipe.stop();
    
    return EXIT_SUCCESS;
}
catch (const rs2::error & e)
{
    std::cerr << "RealSense error calling " << e.get_failed_function() << "(" << e.get_failed_args() << "):\n    " << e.what() << std::endl;
    return EXIT_FAILURE;
}
catch (const std::exception& e)
{
    std::cerr << "Standard error: " << e.what() << std::endl;
    return EXIT_FAILURE;
}
catch (...)
{
    std::cerr << "Unknown error occurred" << std::endl;
    return EXIT_FAILURE;
}

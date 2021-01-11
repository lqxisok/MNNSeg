cmake_minimum_required(VERSION 3.10)
project(Seg_MNN)

set(CMAKE_CXX_STANDARD 11)

find_package(OpenCV REQUIRED)
include_directories(
        include
        src
)

link_directories(lib)

add_executable(segmnn src/main.cpp src/segmnn.cpp)
target_link_libraries(segmnn MNN ${OpenCV_LIBS})
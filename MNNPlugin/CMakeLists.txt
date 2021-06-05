cmake_minimum_required(VERSION 3.10)
project(Seg_MNN)

set(CMAKE_CXX_STANDARD 11)

include_directories(
        include
        src
        3rd_party/imageHelper
        demo
)

link_directories(lib)

add_library(segexp SHARED src/segmnn.cpp src/export.cpp)
target_link_libraries(segexp MNN)

add_executable(segmnn src/main.cpp)
target_link_libraries(segmnn segexp)

add_executable(imageRotate demo/imageRotate.cpp)
target_link_libraries(imageRotate segexp)
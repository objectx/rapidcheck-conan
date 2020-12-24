
cmake_minimum_required (VERSION 3.18)

project (cmake-wrapper)

# message (STATUS "### Wrapper CMakeLists.txt was used! ###")

if (EXISTS "${CMAKE_BINARY_DIR}/conanbuildinfo.cmake")
    include ("${CMAKE_BINARY_DIR}/conanbuildinfo.cmake")
else ()
    include (conanbuildinfo.cmake)
endif ()

conan_basic_setup ()

add_subdirectory (source_subfolder)

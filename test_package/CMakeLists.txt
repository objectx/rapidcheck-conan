cmake_minimum_required (VERSION 3.9)

project (test_package CXX)

include (${CMAKE_BINARY_DIR}/conanbuildinfo.cmake)
conan_basic_setup ()

add_executable (test_package test_package.cpp)
target_link_libraries (test_package ${CONAN_LIBS})
target_compile_features (test_package PRIVATE cxx_std_11)

# CTest is a testing tool that can be used to test your project.
enable_testing ()
add_test (NAME test_package
          WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/bin
          COMMAND test_package)

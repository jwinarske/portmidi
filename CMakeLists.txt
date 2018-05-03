# portmidi
# Roger B. Dannenberg
# 20 Sep 2009

cmake_minimum_required(VERSION 3.0)

# Clear out the built-in C++ compiler and link flags for each of the 
# unused configurations.
set(CMAKE_CXX_FLAGS_MINSIZEREL "" CACHE INTERNAL "Unused")
set(CMAKE_SHARED_LINKER_FLAGS_MINSIZEREL "" CACHE INTERNAL "Unused")
set(CMAKE_MODULE_LINKER_FLAGS_MINSIZEREL "" CACHE INTERNAL "Unused")
set(CMAKE_EXE_LINKER_FLAGS_MINSIZEREL "" CACHE INTERNAL "Unused")
set(CMAKE_CXX_FLAGS_RELWITHDEBINFO "" CACHE INTERNAL "Unused")
set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "" CACHE INTERNAL "Unused")
set(CMAKE_MODULE_LINKER_FLAGS_RELWITHDEBINFO "" CACHE INTERNAL "Unused")
set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "" CACHE INTERNAL "Unused")

set(CMAKE_OSX_ARCHITECTURES i386 ppc x86_64 CACHE STRING "change to needed architecture for a smaller library" FORCE)

PROJECT(portmidi)

include_directories(pm_common porttime)

add_subdirectory(pm_common)

add_subdirectory(pm_test)

add_subdirectory(pm_dylib)

add_subdirectory(pm_java)


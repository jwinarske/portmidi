
project(pyportmidi)

set(CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")

find_package(Cython)

if(CYTHON_FOUND)

    find_package(PythonLibs)
    
    if(PYTHONLIBS_FOUND)

        include( UseCython )
        
        include_directories(
            "${CMAKE_CURRENT_SOURCE_DIR}/../../pm_common"
            "${CMAKE_CURRENT_SOURCE_DIR}/../../porttime"
        )
        cython_add_module( _pyportmidi _pyportmidi.pyx )

        install(TARGETS _pyportmidi DESTINATION ${CMAKE_INSTALL_PREFIX}/lib)
    endif()

endif()

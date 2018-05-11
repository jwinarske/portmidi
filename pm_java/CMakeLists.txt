# pm_java

include(FindJava)
include(FindJNI)
include(UseJava)


if(JAVA_FOUND)

    set(CMAKE_JAVA_COMPILE_FLAGS -Xlint)

    set(JAVA_SOURCE_FILES
        jportmidi/JPortMidi.java
        jportmidi/JPortMidiException.java
        jportmidi/JPortMidiApi.java
        pmdefaults/PmDefaultsFrame.java
        pmdefaults/PmDefaults.java
        pmdefaults/portmusic_logo.png
    )

    if(Java_VERSION_MINOR LESS 10)

        if(${CMAKE_VERSION} VERSION_LESS "3.11.1") 

            add_jar(pmdefaults ${JAVA_SOURCE_FILES}
              OUTPUT_NAME pmdefaults
              MANIFEST pmdefaults/manifest.txt
              DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/pmjni"
            )
            create_javah(
              TARGET pmdefaults_jni_header
              CLASSES jportmidi.JPortMidiApi
              CLASSPATH ${CMAKE_CURRENT_SOURCE_DIR}
              OUTPUT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/pmjni
            )
        else()
            add_jar(pmdefaults ${JAVA_SOURCE_FILES}
              OUTPUT_NAME pmdefaults
              MANIFEST pmdefaults/manifest.txt
              GENERATE_NATIVE_HEADERS pmdefaults_jni_header 
              DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/pmjni"
            )
        endif()
    else()

        if(${CMAKE_VERSION} VERSION_LESS "3.11.1") 
            message(ERROR, "To support JNI header generation with JDK 1.10, you need to upgrade to t least CMake 3.11.1")
        else()

            add_jar(pmdefaults ${JAVA_SOURCE_FILES}
              OUTPUT_NAME pmdefaults
              MANIFEST pmdefaults/manifest.txt
              GENERATE_NATIVE_HEADERS pmdefaults_jni_header 
              DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/pmjni"
            )
        endif()        
    endif()

    install(FILES ${CMAKE_CURRENT_BINARY_DIR}/pmdefaults.jar 
        DESTINATION ${CMAKE_INSTALL_PREFIX}/lib 
    )


    ## jni library ##

    if(APPLE)

        set(JAVAVM_LIB "${FRAMEWORK_PATH}/JavaVM.framework")
        set(JAVA_INCLUDE_PATHS ${JAVAVM_LIB}/Headers)

    elseif(WIN32)

        set(JAVA_INCLUDE_PATHS ${JAVA_INCLUDE_PATH} ${JAVA_INCLUDE_PATH2})
        add_definitions(-D_CRT_SECURE_NO_WARNINGS)

    else()

        message(STATUS "JAVA_INCLUDE_PATH is " ${JAVA_INCLUDE_PATH})
        message(STATUS "JAVA_INCLUDE_PATH2 is " ${JAVA_INCLUDE_PATH2})
        message(STATUS "JAVA_JVM_LIBRARY is " ${JAVA_JVM_LIBRARY})
        set(JAVA_INCLUDE_PATHS ${JAVA_INCLUDE_PATH} ${JAVA_INCLUDE_PATH2})
        # libjvm.so is found relative to JAVA_INCLUDE_PATH:
        set(JAVAVM_LIB ${JAVA_JVM_LIBRARY}/libjvm.so)

    endif()

    include_directories(
        ${JAVA_INCLUDE_PATHS}
        ${CMAKE_CURRENT_SOURCE_DIR}/pmjni/
    )
    set(PMJNI_SOURCE 
        ${CMAKE_CURRENT_SOURCE_DIR}/pmjni/pmjni.c
    )

    add_library(pmjni SHARED ${LIBSRC} ${PMJNI_SOURCE})
    target_link_libraries(pmjni portmidi-static ${JNI_EXTRA_LIBS})

    add_dependencies(pmjni pmdefaults_jni_header)
    add_dependencies(pmjni pmdefaults)
    add_dependencies(pmjni portmidi-static)

    if(UNIX)
        INSTALL(TARGETS pmjni
            LIBRARY DESTINATION ${CMAKE_INSTALL_PREFIX}/lib
            ARCHIVE DESTINATION ${CMAKE_INSTALL_PREFIX}/lib)
    endif(UNIX)

endif(JAVA_FOUND)


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

    if(NOT Java_VERSION_MINOR VERSION_LESS 1.8)

        add_jar(pmdefaults ${JAVA_SOURCE_FILES}
            OUTPUT_NAME pmdefaults
            MANIFEST pmdefaults/manifest.txt
            GENERATE_NATIVE_HEADERS pmdefaults_jni_header 
            DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/pmjni"
        )

    else()
        MESSAGE(ERROR "JNI Header Creation not implemented for this JDK version yet...")
    endif()

    install(FILES ${CMAKE_CURRENT_BINARY_DIR}/pmdefaults.jar 
        DESTINATION ${CMAKE_INSTALL_PREFIX}/lib 
    )

endif(JAVA_FOUND)


if(APPLE)

    set(JAVAVM_LIB "${FRAMEWORK_PATH}/JavaVM.framework")
    set(JAVA_INCLUDE_PATHS ${JAVAVM_LIB}/Headers)

elseif(WIN32)

    set(JAVA_INCLUDE_PATHS ${JAVA_INCLUDE_PATH} ${JAVA_INCLUDE_PATH2})
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)

else()

    message(STATUS "JAVA_JVM_LIB_PATH is " ${JAVA_JVM_LIB_PATH})
    message(STATUS "JAVA_INCLUDE_PATH is " ${JAVA_INCLUDE_PATH})
    message(STATUS "JAVA_INCLUDE_PATH2 is " ${JAVA_INCLUDE_PATH2})
    message(STATUS "JAVA_JVM_LIBRARY is " ${JAVA_JVM_LIBRARY})
    set(JAVA_INCLUDE_PATHS ${JAVA_INCLUDE_PATH} ${JAVA_INCLUDE_PATH2})
    # libjvm.so is found relative to JAVA_INCLUDE_PATH:
    set(JAVAVM_LIB ${JAVA_JVM_LIBRARY}/libjvm.so)

endif()

set(JNI_EXTRA_LIBS ${PM_NEEDED_LIBS} ${JAVA_JVM_LIBRARY})

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

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

    set(CMAKE_JNI_TARGET TRUE)

    add_jar(pmdefaults ${JAVA_SOURCE_FILES}
        OUTPUT_NAME pmdefaults
        OUTPUT_DIR ${CMAKE_BINARY_DIR}/java
        MANIFEST pmdefaults/manifest.txt
        GENERATE_NATIVE_HEADERS pmdefaults_jni_header
    )

    install_jar(pmdefaults DESTINATION ${CMAKE_INSTALL_PREFIX}/bin)

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

include_directories(${JAVA_INCLUDE_PATHS})
add_library(pmjni SHARED ${LIBSRC} ../pm_java/pmjni/pmjni.c)
target_link_libraries(pmjni pmdefaults_jni_header portmidi-static ${JNI_EXTRA_LIBS})

add_dependencies(pmjni pmdefaults)
add_dependencies(pmjni portmidi-static)


# install the libraries (Linux and Mac OS X command line)
if(UNIX)
  INSTALL(TARGETS pmjni
    LIBRARY DESTINATION ${CMAKE_INSTALL_PREFIX}/lib
    ARCHIVE DESTINATION ${CMAKE_INSTALL_PREFIX}/lib)
endif(UNIX)

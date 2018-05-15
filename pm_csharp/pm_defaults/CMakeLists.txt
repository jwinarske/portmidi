cmake_minimum_required(VERSION 3.8)

project(PmDefaults LANGUAGES CSharp)

include(CSharpUtilities)

set(PmDefaults_SOURCE_FILES

    App.config
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs

    Properties/AssemblyInfo.cs

    csportmidi/Defines.cs
    csportmidi/CsPortMidi.cs
    csportmidi/CsPortMidiApi.cs
    csportmidi/CsPortMidiException.cs

    pmdefaults.ico
    portmusic_logo.png
)

set(PmDefaults_PROPERTIES
    Properties/AssemblyInfo.cs
)

set(PmDefaults_XAML_CS_PROPERTIES
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
)

csharp_set_designer_cs_properties(${PmDefaults_PROPERTIES})
csharp_set_xaml_cs_properties(${PmDefaults_XAML_CS_PROPERTIES})

set_property(SOURCE App.xaml PROPERTY VS_XAML_TYPE "ApplicationDefinition")

add_executable(${PROJECT_NAME} ${PmDefaults_SOURCE_FILES})

set(REFS "Microsoft.CSharp;PresentationCore;PresentationFramework")
set(REFS "${REFS};System;System.Core;System.Data;System.Data.DataSetExtensions")
set(REFS "${REFS};System.Net.Http;System.Xaml;System.Xml;System.Xml.Linq")
set(REFS "${REFS};WindowsBase;System.Drawing")

set_target_properties(${PROJECT_NAME} PROPERTIES
    WIN32_EXECUTABLE TRUE
	VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.6.1"
    VS_GLOBAL_AllowUnsafeBlocks True
    VS_DOTNET_REFERENCES "${REFS}"
)

if(MSVC)
    set(CONFIG_DIR "$<CONFIG>")
else()
    if(CMAKE_BUILD_TYPE)
        set(CONFIG_DIR "${CMAKE_BUILD_TYPE}")
    else()
        set(CONFIG_DIR "Debug")
   endif()
endif()

						
if(MSVC)
	if(${CMAKE_VS_PLATFORM_NAME} STREQUAL "x64")
	elseif(${CMAKE_VS_PLATFORM_NAME} STREQUAL "Win32")
	elseif(${CMAKE_VS_PLATFORM_NAME} STREQUAL "ARM")
	endif()
endif()

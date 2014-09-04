@echo off

set PROJECT_NAME=OpenCV Debugger Visualizers
set PROJECT_VERSION=0.5.1
set PROJECT_BINARYDIR=%~dp0\..
set PROJECT_PLATFORM=x86
set COMMAND_ARGS="-dProductName=%PROJECT_NAME%" -dVersion=%PROJECT_VERSION% ^
  "-dProjectBinaryDir=%PROJECT_BINARYDIR%" -dPlatform=%PROJECT_PLATFORM%
set PATH=%PATH%;%WIX%\bin
set OUTPUT_DIR=.

del *.wixobj 2>NUL && ^
candle %COMMAND_ARGS% *.wxs && ^
light %COMMAND_ARGS% *.wixobj -ext WixUIExtension -ext WixVSExtension ^
	-loc strings.en-US.wxl -cultures:en-US ^
	-o "%PROJECT_NAME% %PROJECT_VERSION%.msi"

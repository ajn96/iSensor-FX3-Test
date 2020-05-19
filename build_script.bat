@echo off

@echo Build started > build_log.txt

@echo Updating test repository... >> build_log.txt
git pull

@echo Removing existing build artifacts >> build_log.txt

rmdir /S /Q %CD%\iSensor-FX3-API\bin
rmdir /S /Q %CD%\Resources
rmdir /S /Q %CD%\Test\bin
copy /y %CD%\build_failed.png %CD%\test_status.png

@echo Fetching fresh copy of API... >> build_log.txt

IF EXIST %CD%\iSensor-FX3-API (
cd iSensor-FX3-API\
git fetch
git reset --hard origin/master
cd..
) ELSE (
git clone https://github.com/juchong/iSensor-FX3-API.git
)

@echo Building FX3-API... >> build_log.txt
cd iSensor-FX3-API\
msbuild FX3Api.vbproj /p:configuration=debug
cd..

@echo Copying output... >> build_log.txt
mkdir %CD%\Resources
xcopy /s /Y %CD%\iSensor-FX3-API\bin\Debug %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\boot_fw.img %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\FX3_Firmware.img %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\USBFlashProg.img %CD%\Resources

@echo Building tests... >> build_log.txt
cd Test\
msbuild iSensor-FX3-Test.csproj /p:configuration=debug
cd..

@echo Running tests...
NUnit-2.6.4\bin\nunit-console-x86.exe /framework:net-4.5 /out:%CD%\test_console.txt /xml:%CD%\test_result.xml %CD%\Test\bin\Debug\iSensor-FX3-Test.dll >> build_log.txt

@echo Parsing test result... >> build_log.txt
NUnitLogParser.exe %CD%\test_result.xml %CD%\test_status.png -verbose  >> build_log.txt

@echo Pushing results...
git add test_result.xml
git add test_console.txt
git add test_status.png
git add build_log.txt
git commit -m "Test run results, %DATE% %TIME%"
git push

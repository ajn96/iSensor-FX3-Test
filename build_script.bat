@echo Updating test repository...
git pull

@echo Removing existing build artifacts

rmdir /S /Q %CD%\iSensor-FX3-API\bin
rmdir /S /Q %CD%\Resources
rmdir /S /Q %CD%\Test\bin

@echo Fetching fresh copy of API...

IF EXIST %CD%\iSensor-FX3-API (
cd iSensor-FX3-API\
git pull
cd..
) ELSE (
git clone https://github.com/juchong/iSensor-FX3-API.git
)

@echo Building FX3-API...
cd iSensor-FX3-API\
msbuild FX3Api.vbproj /p:configuration=debug
cd..

@echo Copying output...
mkdir %CD%\Resources
xcopy /s /Y %CD%\iSensor-FX3-API\bin\Debug %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\boot_fw.img %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\FX3_Firmware.img %CD%\Resources
xcopy /s /Y /i %CD%\iSensor-FX3-API\Resources\USBFlashProg.img %CD%\Resources

@echo Building tests...
cd Test\
msbuild iSensor-FX3-Test.csproj /p:configuration=debug
cd..

@echo Running tests...
NUnit-2.6.4\bin\nunit-console-x86.exe /xml:%CD%\test_result.xml /out:%CD%\test_console.txt %CD%\Test\bin\Debug\iSensor-FX3-Test.dll

@echo Pushing results...
cd..
git add -A
git commit -m "Test run results, %DATE% %TIME%"
git push

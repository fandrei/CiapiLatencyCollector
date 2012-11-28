@rem ---------------------------------------------------------------------------------
@rem if user does not set MsDeployPath environment variable, we will try to retrieve it from registry.
@rem ---------------------------------------------------------------------------------
if "%MSDeployPath%" == "" (
for /F "usebackq tokens=1,2,*" %%h  in (`reg query "HKLM\SOFTWARE\Microsoft\IIS Extensions\MSDeploy" /s  ^| findstr -i "InstallPath"`) do (
if /I "%%h" == "InstallPath" ( 
if /I "%%i" == "REG_SZ" ( 
if not "%%j" == "" ( 
if "%%~dpj" == "%%j" ( 
set MSDeployPath=%%j
))))))

"%MSDeployPath%\msdeploy.exe" -verb:sync -source:contentpath="%WORKSPACE%/_UpdatePackage" -skip:skipaction='Delete',objectname='dirPath',absolutepath='/plugins/.*' -skip:skipaction='Delete',objectname='filePath',absolutepath='/plugins/.*' -dest:contentPath="%1"
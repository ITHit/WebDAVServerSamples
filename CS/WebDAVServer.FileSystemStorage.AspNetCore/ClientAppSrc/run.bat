@echo off 
SET isNetFramework=%1
SET isGsuite=%2

echo ".netframework:  %isNetFramework%"
echo "gsuite:  %isGsuite%"

if exist .\node_modules\webdav.client (
    for /f %%i in ('node -p "require('webdav.client/package.json').version" ') do set CurrentVersion=%%i
    for /f %%i in ('npm show webdav.client version ') do set NewVersion=%%i
)

echo "Current Version in project.json %CurrentVersion%"
echo "New Version in project.json %NewVersion%"

IF "%NewVersion%"=="%CurrentVersion%" (
    echo "webdav.client version is up to date"
    if not exist ..\wwwroot\app.js (
        call npm install
        call:buildApp
        call:copyClient
    ) 
    ) else (
    echo "Found new webdav.client version %NewVersion%"
    call npm install webdav.client --save
    call:buildApp
    call:copyClient )
exit /b 0


:buildApp 
if "%isNetFramework%"=="true" (
    if "%isGsuite%"=="true" (
        call npm run build:netframework:gsuite
    ) else (
        call npm run build:netframework
    )
) else (
        if "%isGsuite%"=="true" (
        call npm run build:gsuite
    ) else (
        call npm run build
    )
)
exit /b 0


:copyClient
    if not exist "..\wwwroot\webdav.client" ( 
        mkdir "..\wwwroot\webdav.client"
    )
    robocopy "node_modules\webdav.client" "..\wwwroot\webdav.client" /E
exit /b 0
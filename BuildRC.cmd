@echo off
call "C:\PROGRA~1\MICROS~4\2022\COMMUN~1\VC\AUXILI~1\Build\vcvars64.bat"
if "%2"=="" (SET "RN=%1") ELSE (SET "RN=%2")
rc.exe /nologo /fo %RN%.res %1.rc
link.exe /DLL /NOENTRY /NODEFAULTLIB /MACHINE:iX86 /OUT:%RN%.dll %RN%.res
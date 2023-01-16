@echo off

FOR /d /r %%F IN (obj?) DO (
    echo deleting folder: %%F
    @IF EXIST %%F RMDIR /S /Q "%%F"
)

FOR /d /r %%F IN (bin?) DO (
    echo deleting folder: %%F
    @IF EXIST %%F RMDIR /S /Q "%%F"
)

FOR /d /r %%F IN (Backup?) DO (
    echo deleting folder: %%F
    @IF EXIST %%F RMDIR /S /Q "%%F"
)

FOR /d /r %%F IN (x86?) DO (
    echo deleting folder: %%F
    @IF EXIST %%F RMDIR /S /Q "%%F"
)

FOR /d /r %%F IN (x64?) DO (
    echo deleting folder: %%F
    @IF EXIST %%F RMDIR /S /Q "%%F"
)
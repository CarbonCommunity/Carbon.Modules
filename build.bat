@echo off

mkdir "%~dp0.release" 2>NUL
del /q "%~dp0.release\*.*" 2>NUL

for /d %%D in (%~dp0*) do (
	echo "%%D" | findstr /i /L ".release" >NUL
	if errorlevel 1 (
		dotnet build "%%D" --configuration Release --nologo
		copy "%%D\bin\Release\*.exe" "%~dp0.release"
	)
)
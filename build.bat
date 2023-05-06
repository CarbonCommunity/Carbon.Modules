@echo off

(
	mkdir "%~dp0.release"
	del /q "%~dp0.release\*.*"
) 2>NUL

dotnet build %~dp0\ExampleModule /t:DownloadToolsTask --nologo

for /r %~dp0 %%D in (*.csproj) do (
	dotnet build "%%D" --configuration Release --nologo
	copy "%%~pDbin\Release\*.dll" "%~dp0.release"
)

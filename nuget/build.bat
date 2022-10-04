@setlocal
@echo off

REM This builds the ResgateIO Client NuGet package.

pushd ..\ResgateIO.Client

dotnet >nul 2>&1
if errorlevel 9009 if not errorlevel 9010 (
	echo 'dotnet.exe' is not in the path.
	goto End
)

dotnet pack ResgateIO.Client.csproj --configuration Release --include-symbols

move bin\Release\*.nupkg ..\nuget 1>NUL
move bin\Release\*.snupkg ..\nuget 1>NUL

:End

popd

echo.
echo Built with .NET Core SDK version:
dotnet --version

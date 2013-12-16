@echo off

set THIS_DIR=%~d0%~p0
REM set THIS_DIR=F:\src\github_jm\metaheuristics\build\
set COPYOPTIONS=/Y /R /D

nuget pack %THIS_DIR%..\TIME.Metaheuristics.Parallel\TIME.Metaheuristics.Parallel.csproj -IncludeReferencedProjects

if errorlevel 1 goto bailOut

goto success 

:noSrcNuspec
echo not found: source nuspec not found
exit /B 1

:bailOut
echo ERROR
exit /B 1

:success
echo Done!

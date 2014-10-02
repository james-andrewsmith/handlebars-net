$solutionDir = [System.IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
$path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

$NativeAssembliesDir = Join-Path $path "tools\native"
$x86 = $(Join-Path $NativeAssembliesDir "x86\*.*")
$x64 = $(Join-Path $NativeAssembliesDir "amd64\*.*")

$PostBuildCmd = "
if not exist `"`$(TargetDir)`" md `"`$(TargetDir)`"
xcopy /s /y `"$x86`" `"`$(TargetDir)`"
if not exist `"`$(TargetDir)`" md `"`$(TargetDir)`"
xcopy /s /y `"$x64`" `"`$(TargetDir)`""
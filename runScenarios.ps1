[CmdletBinding()]
Param(
    [ValidateSet('NoBatchCausingUnreachable','WithBatchCausingUnreachable','WithBatchNoUnreachable')]
    $scenario = 'NoBatchCausingUnreachable',
    [switch]
    $buildBeforeRun
)

Get-Process | Where-Object {$_.MainWindowTitle -like '*TestAkkaClusterLighthouseExe'} | Stop-Process
Get-Process | Where-Object {$_.MainWindowTitle -like '*TestAkkaClusterPerformanceExe'} | Stop-Process

if ($buildBeforeRun -eq $true)
{
    dotnet publish '.\src\Lighthouse\Lighthouse.csproj' --framework netcoreapp3.1 -c Release -o '.\publish\Lighthouse'
    dotnet publish '.\src\TestAkkaCluster\TestAkkaClusterPerformance.csproj' --framework net6.0 -c Release -o '.\publish\TestAkkaClusterPerformance'
}

$NoOfWorkers = 1000
$DoWarmup = $true
$ShiftInMs = 500
$SendInBatches = $false
$BatchSize = 50
$BatchDelayInMs = 200
$StartAfterInSeconds = 10

if ($scenario -ne 'NoBatchCausingUnreachable')
{
    $SendInBatches = $true
}

if ($scenario -eq 'WithBatchNoUnreachable')
{
    $BatchSize = 10
    $BatchDelayInMs = 500
}

$LighthousePath = Resolve-Path '.\publish\Lighthouse\'
$LighthouseDll = Resolve-Path '.\publish\Lighthouse\Lighthouse.dll'
cmd.exe /c "start ""TestAkkaClusterLighthouseExe"" powershell.exe -NoExit -Command ""Set-Location $LighthousePath; dotnet $LighthouseDll"""

$TestParams = "--NoOfWorkers=$NoOfWorkers --DoWarmup=$DoWarmup --ShiftInMs=$ShiftInMs --SendInBatches=$SendInBatches --BatchSize=$BatchSize --BatchDelayInMs=$BatchDelayInMs --StartAfterInSeconds=$StartAfterInSeconds"
$TestAkkaClusterPerformancePath = Resolve-Path '.\publish\TestAkkaClusterPerformance'
$TestAkkaClusterPerformanceDll = Resolve-Path '.\publish\TestAkkaClusterPerformance\TestAkkaClusterPerformance.dll'
cmd.exe /c "start  ""TestAkkaClusterPerformanceExe"" powershell.exe -NoExit -Command ""Set-Location $TestAkkaClusterPerformancePath; dotnet $TestAkkaClusterPerformanceDll $TestParams"" -WorkingDirectory $TestAkkaClusterPerformancePath"

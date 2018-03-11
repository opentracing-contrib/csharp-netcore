[CmdletBinding(PositionalBinding=$false)]
param(
    # This number will be appended to all nuget package versions.
    # This should be overwritten by a CI system like VSTS, AppVeyor, TeamCity, ...
    [string] $VersionSuffix = "loc" + ((Get-Date).ToUniversalTime().ToString("yyyyMMddHHmm")),

    # The folder in which all output packages should be placed.
    [string] $ArtifactsPath = (Join-Path $PWD "artifacts"),

    [bool] $RunClean = $true,
    [bool] $RunBuild = $true,
    [bool] $RunTests = $true
)

$BuildConfiguration = "Release"
$ArtifactsPathNuGet = "nuget"

$Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()


function Task {
    [CmdletBinding()] param (

        [Parameter(Mandatory = $true)] [string] $name,
        [Parameter(Mandatory = $false)] [bool] $runTask,
        [Parameter(Mandatory = $false)] [scriptblock] $cmd
    )

    Assert ($cmd -ne $null) "Command is missing for task '$name'. Make sure the starting '{' is on the same line as the term 'Task'. E.g. 'Task `"$name`" `$Run$name {'"

    if ($runTask -eq $true) {
        Write-Host "`n------------------------- [$name] -------------------------`n" -ForegroundColor Cyan

        $sw = [System.Diagnostics.Stopwatch]::StartNew()

        & $cmd

        Write-Host "`nTask '$name' finished in $($sw.Elapsed.TotalSeconds) sec."
    }
    else {
        Write-Host "`n------------------ Skipping task '$name' ------------------" -ForegroundColor Yellow
    }
}



Write-Host "`n------------------------ [Settings] -----------------------`n" -ForegroundColor Cyan

Write-Host "VersionSuffix: $VersionSuffix"
Write-Host "BuildConfiguration: $BuildConfiguration"
Write-Host "ArtifactsPath: $ArtifactsPath"
Write-Host "ArtifactsPathNuGet: $ArtifactsPathNuGet"

Assert ($BuildConfiguration -ne $null) "Property 'BuildConfiguration' may not be null."
Assert ($ArtifactsPath -ne $null) "Property 'ArtifactsPath' may not be null."
Assert ($ArtifactsPathNuGet -ne $null) "Property 'ArtifactsPathNuGet' may not be null."

Assert ((Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -ne $null) ".NET Core SDK is not installed (dotnet.exe not found)."


Task "Clean" $RunClean {
    if (Test-Path $ArtifactsPath) {
        Write-Host "Removing existing artifacts folder '$ArtifactsPath'"
        Remove-Item -Path $ArtifactsPath -Recurse -Force -ErrorAction Ignore
    }

    New-Item $ArtifactsPath -ItemType Directory -ErrorAction Ignore | Out-Null
    Write-Host "Created artifacts folder '$ArtifactsPath'"
}


Task "Build" $RunBuild {

    $packageOutputPath = Join-Path $ArtifactsPath $ArtifactsPathNuGet

    dotnet msbuild "/t:Restore;Build;Pack" "/p:Configuration=$BuildConfiguration" "/p:PackageOutputPath=$packageOutputPath" "/p:CI=true" "/p:VersionSuffix=$VersionSuffix"
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed. See output for details."
    }
}


Task "Tests" $RunTests {

    $testsFailed = $false

    Get-ChildItem .\test -Filter *.csproj -Recurse | ForEach-Object {

        $library = Split-Path $_.DirectoryName -Leaf
        Write-Host "`nTesting $library`n"

        Push-Location $_.Directory
        dotnet xunit -configuration $BuildConfiguration -nobuild
        Pop-Location

        if ($LASTEXITCODE -ne 0) {
            $testsFailed = $true
        }
    }

    if ($testsFailed) {
        throw "At least one test failed. See output for details."
    }
}


Write-Host "`nBuild finished in $($Stopwatch.Elapsed.TotalSeconds) sec." -ForegroundColor Green

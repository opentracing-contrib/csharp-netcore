[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $Framework = "netcoreapp2.1"
)

dotnet build
if ($LASTEXITCODE -ne 0) { throw "build error" }

Write-Host "Launching samples with framework $Framework"

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run -f $Framework --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\CustomersApi"

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run -f $Framework --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\OrdersApi"

Start-Sleep -Seconds 2

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run -f $Framework --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\FrontendWeb"

if ($Framework -ne "netcoreapp2.0") {
    Start-Process `
        -FilePath powershell.exe `
        -ArgumentList @( "dotnet run -f $Framework --no-build; Read-Host 'Press enter to exit'" ) `
        -WorkingDirectory "samples\TrafficGenerator"
}

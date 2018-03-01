dotnet build
if ($LASTEXITCODE -ne 0) { throw "build error" }

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run --no-restore --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\CustomersApi"

Start-Sleep -Seconds 2

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run --no-restore --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\OrdersApi"

Start-Sleep -Seconds 2

Start-Process `
    -FilePath powershell.exe `
    -ArgumentList @( "dotnet run --no-restore --no-build; Read-Host 'Press enter to exit'" ) `
    -WorkingDirectory "samples\FrontendWeb"

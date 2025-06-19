Push-Location "src/redmuffin.Blazor.StaticWeb.Api/"
    Start-Process -NoNewWindow "C:\Program Files\Microsoft Visual Studio\2022\Preview\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator\azurite.exe"
    Start-Process -NoNewWindow powershell -ArgumentList "-Command dotnet run --port 7184"
Pop-Location

Push-Location "src/redmuffin.Blazor.StaticWeb/"
    Start-Process -NoNewWindow powershell -ArgumentList "-Command dotnet run"
Pop-Location

swa start http://localhost:5233 --api-location http://localhost:7184/api

$html = Get-Content 'B:\redmuffin.Blazor.StaticWeb\TestResults\redmuffin.Blazor.StaticWeb.Tests-windows-net10.0-report.html' -Raw
$b64 = ($html -split '<script id="report-data" type="application/octet-stream">')[1] -split '</script>'
$bytes = [Convert]::FromBase64String($b64[0].Trim())
$gzip = [IO.Compression.GzipStream]::new([IO.MemoryStream]::new($bytes), [IO.Compression.CompressionMode]::Decompress)
$json = ([IO.StreamReader]::new($gzip)).ReadToEnd()
$data = $json | ConvertFrom-Json

$results = @()
foreach ($test in $data.tests) {
    $results += [PSCustomObject]@{
        ClassName  = "$($test.ns).$($test.cls)"
        TestName   = $test.name
        DurationMs = [math]::Round($test.duration, 2)
    }
}

$results | Sort-Object DurationMs -Descending | Format-Table -AutoSize
$results | Sort-Object DurationMs -Descending | Export-Csv -Path 'B:\redmuffin.Blazor.StaticWeb\scripts\test-durations.csv' -NoTypeInformation
Write-Host "Total tests: $($results.Count)"
Write-Host "Total duration: $([math]::Round(($results | Measure-Object DurationMs -Sum).Sum, 0))ms"

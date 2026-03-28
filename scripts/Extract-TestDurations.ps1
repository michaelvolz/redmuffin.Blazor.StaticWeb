$html = Get-Content 'B:\redmuffin.Blazor.StaticWeb\TestResults\redmuffin.Blazor.StaticWeb.Tests-windows-net9.0-report.html' -Raw
$json = ($html -split '<script id="test-data" type="application/json">')[1] -split '</script>'
$data = $json[0] | ConvertFrom-Json

$results = @()
foreach ($group in $data.groups) {
    foreach ($test in $group.tests) {
        $results += [PSCustomObject]@{
            ClassName = $test.className
            TestName = $test.displayName
            DurationMs = [math]::Round($test.durationMs, 2)
        }
    }
}

$results | Sort-Object DurationMs -Descending | Format-Table -AutoSize
$results | Sort-Object DurationMs -Descending | Export-Csv -Path 'B:\redmuffin.Blazor.StaticWeb\scripts\test-durations.csv' -NoTypeInformation
Write-Host "Total tests: $($results.Count)"
Write-Host "Total duration: $($data.summary.totalDurationMs)ms"
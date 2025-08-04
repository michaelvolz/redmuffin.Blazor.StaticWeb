$results = @()

Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse | 
    Where-Object { $_.Name -notlike "*.Helpers.cs" -and $_.FullName -notmatch "obj\\" } | 
    ForEach-Object {
        $file = $_
        $content = Get-Content $file.FullName -Raw
        
        # Extract namespace
        $namespace = ""
        if ($content -match "namespace\s+([\w\.]+)") {
            $namespace = $matches[1]
        }
        
        # Find all [Test] methods
        $testMatches = [regex]::Matches($content, '\[Test\]\s*\r?\n\s*public\s+async\s+Task\s+(\w+)\s*\(')
        foreach ($match in $testMatches) {
            $results += [PSCustomObject]@{
                Namespace = $namespace
                MethodName = $match.Groups[1].Value
                Annotation = "[Test]"
                FilePath = $file.FullName
            }
        }
        
        # Find all [Arguments] methods  
        $argMatches = [regex]::Matches($content, '\[Arguments.*?\]\s*(?:\[Arguments.*?\]\s*)*public\s+async\s+Task\s+(\w+)\s*\(')
        foreach ($match in $argMatches) {
            $results += [PSCustomObject]@{
                Namespace = $namespace
                MethodName = $match.Groups[1].Value
                Annotation = "[Arguments]"
                FilePath = $file.FullName
            }
        }
    }

$results | ConvertTo-Json -Depth 3 | Set-Content -Path "test-inventory.json"
Write-Host "Found $($results.Count) test methods. Output saved to test-inventory.json"

Describe 'List-Sidenotes.ps1' {
    BeforeEach {
        $script:ScriptPath = Join-Path $PSScriptRoot 'List-Sidenotes.ps1'
        $script:TestSidenotesPath = Join-Path $TestDrive 'sidenotes'
        New-Item -ItemType Directory -Path $script:TestSidenotesPath -Force | Out-Null
    }

    AfterEach {
        if (Test-Path $script:TestSidenotesPath) {
            Remove-Item $script:TestSidenotesPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'lists pending sidenotes in numeric order from frontmatter' {
        @'
---
id: SN-0005
date: 2026-04-05
title: Fifth note
status: pending
---
Body 5
'@ | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-0005.md') -Encoding UTF8

        @'
---
id: SN-0002
date: 2026-04-05
title: Second note
status: pending
---
Body 2
'@ | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-0002.md') -Encoding UTF8

        $output = & $script:ScriptPath -SidenotesPath $script:TestSidenotesPath *>&1 | ForEach-Object { $_.ToString() }

        $output[0] | Should Be 'Pending sidenotes:'
        $output[1] | Should Be '1. SN-0002 (2026-04-05) - Second note'
        $output[2] | Should Be '2. SN-0005 (2026-04-05) - Fifth note'
    }

    It 'skips non-pending sidenotes and returns the empty state when nothing is pending' {
        @'
---
id: SN-0001
date: 2026-04-05
title: Converted note
status: converted
---
Body
'@ | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-0001.md') -Encoding UTF8

        $output = & $script:ScriptPath -SidenotesPath $script:TestSidenotesPath *>&1 | ForEach-Object { $_.ToString() }

        ($output -join "`n") | Should Match 'No pending sidenotes\.'
    }

    It 'marks malformed sidenotes without breaking the list' {
        @'
---
id: SN-0003
date: 2026-04-05
title: Valid pending note
status: pending
---
Body
'@ | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-0003.md') -Encoding UTF8

        'This file has no frontmatter' | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-9999.md') -Encoding UTF8

        $output = & $script:ScriptPath -SidenotesPath $script:TestSidenotesPath *>&1 | ForEach-Object { $_.ToString() }

        ($output -join "`n") | Should Match '\[malformed sidenote\] SN-9999'
        ($output -join "`n") | Should Match '1\. SN-0003 \(2026-04-05\) - Valid pending note'
    }

    It 'emits title length warnings for long frontmatter titles' {
        $longTitle = 'This title is intentionally long so the soft warning path is exercised and remains visible to the user during listing.'

        @"
---
id: SN-0010
date: 2026-04-05
title: $longTitle
status: pending
---
Body
"@ | Set-Content -Path (Join-Path $script:TestSidenotesPath 'SN-0010.md') -Encoding UTF8

        $output = & $script:ScriptPath -SidenotesPath $script:TestSidenotesPath *>&1 | ForEach-Object { $_.ToString() }

        ($output -join "`n") | Should Match '⚠ SN-0010 exceeds the hard title limit:'
        ($output -join "`n") | Should Match '1\. SN-0010 \(2026-04-05\) - '
    }
}

# Pester tests for cleanup-sessions.ps1
# Run: pwsh -NoProfile -Command "Invoke-Pester .config/opencode/scripts/cleanup-sessions.Tests.ps1"

BeforeAll {
    $ScriptPath = Join-Path $PSScriptRoot "cleanup-sessions.ps1"
    . $ScriptPath
}

Describe "cleanup-sessions.ps1" {

    Context "given_opencode_missing_from_PATH" {
        BeforeEach {
            Mock Get-Command { return $null } -ParameterFilter { $Name -eq "opencode" }
        }

        It "throws_with_fatal_error" {
            { Invoke-SessionCleanup -Days 5 } | Should -Throw -ExpectedMessage "*opencode*not found*"
        }
    }

    Context "given_invalid_Days_parameter" {
        It "throws_for_negative_value" {
            { Invoke-SessionCleanup -Days -1 } | Should -Throw -ExpectedMessage "*Days*must be positive*"
        }

        It "throws_for_zero" {
            { Invoke-SessionCleanup -Days 0 } | Should -Throw -ExpectedMessage "*Days*must be positive*"
        }
    }

    Context "given_no_old_sessions_with_WhatIf" {
        BeforeEach {
            Mock Invoke-OpenCodeDb { return @() } -ParameterFilter { $Query -like "*SELECT id, title*" }
            Mock Get-CurrentSessionId { return $null }
        }

        It "reports_nothing_to_delete" {
            $output = Invoke-SessionCleanup -Days 5 -WhatIf 6>&1
            $output | Should -Be "No sessions older than 5 days to delete."
        }
    }

    Context "given_old_sessions_with_WhatIf" {
        BeforeEach {
            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_A"; title = "Test A"; time_updated = 1; parent_id = $null },
                    [PSCustomObject]@{ id = "ses_B"; title = "Test B"; time_updated = 2; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { }
        }

        It "lists_sessions_without_deleting" {
            $output = Invoke-SessionCleanup -Days 5 -WhatIf 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*ses_A*Test A*"
            $joined | Should -BeLike "*ses_B*Test B*"
            $joined | Should -BeLike "*2 session(s)*"
            Should -Invoke Invoke-OpenCodeDelete -Times 0
        }
    }

    Context "given_current_session_auto_excluded" {
        BeforeEach {
            $now = [int64]((Get-Date).ToUniversalTime() - (Get-Date "1970-01-01Z")).TotalMilliseconds
            $old6 = $now - (6 * 86400000)
            $old7 = $now - (7 * 86400000)

            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_current"; title = "Current session"; time_updated = $old6; parent_id = $null },
                    [PSCustomObject]@{ id = "ses_old1"; title = "Old session 1"; time_updated = $old7; parent_id = $null },
                    [PSCustomObject]@{ id = "ses_old2"; title = "Old session 2"; time_updated = $old7 - 1000; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return "ses_current" }
        }

        It "excludes_current_session_from_candidates" {
            $output = Invoke-SessionCleanup -Days 5 -WhatIf 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*ses_old1*"
            $joined | Should -BeLike "*ses_old2*"
            $joined | Should -BeLike "*2 session(s)*"
            ($output | Where-Object { $_ -notlike "*Excluded current*" } | Out-String) | Should -Not -BeLike "*ses_current*"
        }
    }

    Context "given_forked_tree_all_members_old_with_WhatIf" {
        BeforeEach {
            $now = [int64]((Get-Date).ToUniversalTime() - (Get-Date "1970-01-01Z")).TotalMilliseconds
            $old10 = $now - (10 * 86400000)

            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_parent"; title = "Parent"; time_updated = $old10; parent_id = $null },
                    [PSCustomObject]@{ id = "ses_child1"; title = "Child 1"; time_updated = $old10 - 1000; parent_id = "ses_parent" },
                    [PSCustomObject]@{ id = "ses_child2"; title = "Child 2"; time_updated = $old10 - 2000; parent_id = "ses_parent" }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { }
            Mock Invoke-OpenCodeDb { return @() } -ParameterFilter { $Query -like "*parent_id IN*" }
        }

        It "lists_entire_tree_for_deletion" {
            $output = Invoke-SessionCleanup -Days 5 -WhatIf 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*ses_parent*"
            $joined | Should -BeLike "*ses_child1*"
            $joined | Should -BeLike "*ses_child2*"
            $joined | Should -BeLike "*3 session(s)*"
        }
    }

    Context "given_forked_tree_with_recent_child" {
        BeforeEach {
            $now = [int64]((Get-Date).ToUniversalTime() - (Get-Date "1970-01-01Z")).TotalMilliseconds
            $old10 = $now - (10 * 86400000)
            $old1  = $now - (1 * 86400000)

            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_parent"; title = "Parent"; time_updated = $old10; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_child"; title = "Child"; time_updated = $old1; parent_id = "ses_parent" }
                )
            } -ParameterFilter { $Query -like "*parent_id IN*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { }
        }

        It "preserves_entire_tree" {
            $output = Invoke-SessionCleanup -Days 5 -WhatIf 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*skipped*ses_parent*"
            $joined | Should -BeLike "*recent child*"
            Should -Invoke Invoke-OpenCodeDelete -Times 0
        }
    }

    Context "given_successful_deletion" {
        BeforeEach {
            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_X"; title = "Old session"; time_updated = 1; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { }
        }

        It "deletes_each_session_and_reports" {
            $output = Invoke-SessionCleanup -Days 5 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*Deleted: ses_X*Old session*"
            $joined | Should -BeLike "*1 deleted*"
        }
    }

    Context "given_partial_deletion_failure" {
        BeforeEach {
            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_good"; title = "Good"; time_updated = 1; parent_id = $null },
                    [PSCustomObject]@{ id = "ses_bad"; title = "Bad"; time_updated = 2; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { } -ParameterFilter { $SessionId -eq "ses_good" }
            Mock Invoke-OpenCodeDelete { throw "simulated failure" } -ParameterFilter { $SessionId -eq "ses_bad" }
        }

        It "continues_and_reports_failures" {
            $output = Invoke-SessionCleanup -Days 5 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*Deleted: ses_good*"
            $joined | Should -BeLike "*Failed: ses_bad*"
            $joined | Should -BeLike "*1 deleted, 1 failed*"
        }
    }

    Context "given_all_deletions_fail" {
        BeforeEach {
            Mock Invoke-OpenCodeDb {
                return @(
                    [PSCustomObject]@{ id = "ses_fail"; title = "Fail"; time_updated = 1; parent_id = $null }
                )
            } -ParameterFilter { $Query -like "*SELECT id, title*" }

            Mock Get-CurrentSessionId { return $null }
            Mock Invoke-OpenCodeDelete { throw "failure" }
        }

        It "reports_all_failed" {
            $output = Invoke-SessionCleanup -Days 5 6>&1
            $joined = $output -join "`n"
            $joined | Should -BeLike "*Failed: ses_fail*"
            $joined | Should -BeLike "*0 deleted, 1 failed*"
        }
    }

}

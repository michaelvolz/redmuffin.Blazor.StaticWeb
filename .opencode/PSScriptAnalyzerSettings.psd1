@{
    Severity    = @('Error', 'Warning')
    ExcludeRules = @(
        'PSAvoidUsingWriteHost',
        'PSUseShouldProcessForStateChangingFunctions'
    )
    Rules = @{
        PSUseCompatibleSyntax    = @{ TargetVersions = @('7.2') }
        PSUseCompatibleCommands  = @{ TargetVersions = @('7.2') }
    }
}

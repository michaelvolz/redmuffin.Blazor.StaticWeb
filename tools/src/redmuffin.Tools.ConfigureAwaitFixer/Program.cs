using redmuffin.Tools.ConfigureAwaitFixer;

var args_ = Arguments.Parse(args);
if (args_ is null)
    return 1;

// In CI the one-shot pipeline must never edit files. The daemon and the
// --fix client are explicit requests and bypass the CI skip.
if (args_.Mode == FixerMode.OneShot && Arguments.IsRunningInCI())
    return 0;

if (args_.Mode == FixerMode.Fix)
    return await FixClient.RunAsync(args_.SingleFile!).ConfigureAwait(false);

if (args_.Mode == FixerMode.Daemon)
    return await Daemon.RunAsync(args_).ConfigureAwait(false);

return await OneShotRunner.RunAsync(args_).ConfigureAwait(false);

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:29.2278888Z","moduleHash":"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855","forms":[]}
// clj-mutate-manifest-end

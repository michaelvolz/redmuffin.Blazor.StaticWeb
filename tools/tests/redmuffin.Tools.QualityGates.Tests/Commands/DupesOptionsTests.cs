using redmuffin.Tools.QualityGates.Commands;

namespace redmuffin.Tools.QualityGates.Tests.Commands;

[Category("Feature:Dupes")]
public sealed class DupesOptionsTests
{
    [Test]
    public async Task DupesOptions_Defaults_Match_Dry4clj_Defaults()
    {
        var options = new DupesOptions();

        await Assert.That(options.Threshold).IsEqualTo(0.82);
        await Assert.That(options.MinLines).IsEqualTo(4);
        await Assert.That(options.MinNodes).IsEqualTo(20);
        await Assert.That(options.Format).IsEqualTo("text");
        await Assert.That(options.Paths).IsEmpty();
    }

    [Test]
    public async Task DupesOptions_CanOverrideDefaults()
    {
        var options = new DupesOptions(
            Threshold: 0.9,
            MinLines: 10,
            MinNodes: 50,
            Format: "json",
            Paths: ["src/"]);

        await Assert.That(options.Threshold).IsEqualTo(0.9);
        await Assert.That(options.MinLines).IsEqualTo(10);
        await Assert.That(options.MinNodes).IsEqualTo(50);
        await Assert.That(options.Format).IsEqualTo("json");
        await Assert.That(options.Paths).HasSingleItem();
        await Assert.That(options.Paths[0]).IsEqualTo("src/");
    }
}

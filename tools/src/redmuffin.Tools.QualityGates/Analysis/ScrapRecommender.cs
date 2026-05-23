namespace redmuffin.Tools.QualityGates.Analysis;

public static class ScrapRecommender
{
    // Stability thresholds (from scrap policy.clj)
    private const double GeneralMaxScrapStable = 12.0;
    private const double GeneralDupStable = 3.0;
    private const double GeneralLowAssertRatioStable = 0.35;
    private const double SmallFileMaxScrapStable = 10.0;
    private const double SmallFileDupStable = 1.0;
    private const int SmallFileMaxExamples = 2;

    // SPLIT thresholds
    private const double SplitAvgScrap = 10.0;
    private const double SplitDupScore = 20.0;
    private const double SplitSubjectRepetition = 12.0;
    private const int SplitMinExamples = 12;
    private const int SplitMinHighPressureBlocks = 2;
    private const double SplitMaxScrap = 35.0;

    // AI-actionability thresholds
    private const double AutoRefactorLowAssertRatio = 0.40;
    private const double AutoRefactorMaxScrap = 20.0;

    /// <summary>
    /// Decides the stability mode and AI-actionability for a file report.
    /// </summary>
    /// <returns></returns>
    public static Recommendation Decide(FileScrapReport report)
    {
        var mode = ClassifyStability(report);
        var actionability = ActionabilityForMode(mode, report);

        // Case-matrix override
        actionability = ApplyCaseMatrixOverride(report, mode, actionability);

        var message = MessageForActionability(actionability);
        return new Recommendation(mode, actionability, message);
    }

    private static AiActionability ActionabilityForMode(StabilityMode mode, FileScrapReport report) =>
        mode switch
        {
            StabilityMode.Stable => AiActionability.LeaveAlone,
            StabilityMode.Split => AiActionability.ManualSplit,
            StabilityMode.Local => ClassifyLocalActionability(report),
            _ => AiActionability.ReviewFirst,
        };

    private static AiActionability ApplyCaseMatrixOverride(
        FileScrapReport report, StabilityMode mode, AiActionability actionability)
    {
        if (report.DuplicationResults.CaseMatrixRepetition.Count == 0
            || actionability == AiActionability.ManualSplit)
        {
            return actionability;
        }

        var caseMatrixCount = report.DuplicationResults.CaseMatrixRepetition.Count;
        var harmfulCount = report.DuplicationResults.HarmfulDuplication.Count;
        if (caseMatrixCount >= Math.Max(2, harmfulCount / 3)
            && report.MaxScrap <= GeneralMaxScrapStable
            && mode != StabilityMode.Split)
        {
            return AiActionability.AutoTableDrive;
        }

        return actionability;
    }

    private static string MessageForActionability(AiActionability actionability) =>
        actionability switch
        {
            AiActionability.LeaveAlone => "File is stable. No action needed.",
            AiActionability.AutoTableDrive => "Case-matrix pattern detected. Consider table-driven test refactoring.",
            AiActionability.AutoRefactor => "File has fixable smells. In-place refactoring recommended.",
            AiActionability.ManualSplit => "File exceeds SPLIT thresholds. Manual split into focused test files recommended.",
            AiActionability.ReviewFirst => "File needs review. Automated refactoring may not be safe.",
            _ => "Unknown recommendation.",
        };

    private static StabilityMode ClassifyStability(FileScrapReport report)
    {
        if (IsSmallFileStable(report)) return StabilityMode.Stable;
        if (IsGeneralFileStable(report)) return StabilityMode.Stable;

        if (ShouldSplit(report)) return StabilityMode.Split;

        return StabilityMode.Local;
    }

    private static bool IsSmallFileStable(FileScrapReport report)
    {
        return report.ExampleCount <= SmallFileMaxExamples
            && report.MaxScrap <= SmallFileMaxScrapStable
            && report.DuplicationResults.EffectiveDuplicationScore <= SmallFileDupStable
            && report.SmellCounts.ZeroAssertionCount == 0;
    }

    private static bool IsGeneralFileStable(FileScrapReport report)
    {
        return report.ExampleCount > SmallFileMaxExamples
            && report.MaxScrap <= GeneralMaxScrapStable
            && report.DuplicationResults.EffectiveDuplicationScore <= GeneralDupStable
            && report.SmellCounts.ZeroAssertionCount == 0
            && report.SmellCounts.LowAssertionRatio <= GeneralLowAssertRatioStable;
    }

    private static bool ShouldSplit(FileScrapReport report)
    {
        var subjectCount = report.DuplicationResults.SubjectRepetition.Count;
        var subjectRepetitionScore = report.DuplicationResults.SubjectRepetition.Sum(s => s.InstanceCount);
        var highPressureBlocks = report.ExtractionPressure.ClusterPressures.Count(p => p >= 18.0);

        var splitTrigger = report.AvgScrap >= SplitAvgScrap
            || report.DuplicationResults.EffectiveDuplicationScore >= SplitDupScore
            || subjectRepetitionScore >= SplitSubjectRepetition
            || subjectCount > 0;

        return splitTrigger
            && report.ExampleCount >= SplitMinExamples
            && (highPressureBlocks >= SplitMinHighPressureBlocks || report.MaxScrap >= SplitMaxScrap);
    }

    private static AiActionability ClassifyLocalActionability(FileScrapReport report)
    {
        var hasZeroAssertion = report.SmellCounts.ZeroAssertionCount > 0;
        var hasHighLowAssert = report.SmellCounts.LowAssertionRatio > AutoRefactorLowAssertRatio;
        var hasHarmfulDup = report.DuplicationResults.HarmfulDuplication.Count > 0;
        var hasHighScrap = report.MaxScrap > AutoRefactorMaxScrap;

        var qualifiesForAutoRefactor = hasZeroAssertion || hasHighLowAssert || hasHarmfulDup || hasHighScrap;

        if (qualifiesForAutoRefactor)
        {
            return AiActionability.AutoRefactor;
        }

        return AiActionability.ReviewFirst;
    }
}

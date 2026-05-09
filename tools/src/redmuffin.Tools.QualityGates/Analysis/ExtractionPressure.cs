namespace redmuffin.Tools.QualityGates.Analysis;

public static class ExtractionPressure
{
    private const double HelperFormCost = 0.5;
    private const double HelperVariableCost = 0.3;
    private const double MatrixCreditPerCluster = 1.5;

    /// <summary>
    /// Computes D_before using Uncle Bob's extraction pressure formula:
    /// D_before = 0 if F ≤ 3 or V > 4, else max(0, F-3) * (I-1)^1.5 / (V+1)
    /// </summary>
    public static double ComputeDefore(int sharedForms, int instanceCount, int variablePoints)
    {
        if (sharedForms <= 3 || variablePoints > 4)
        {
            return 0.0;
        }

        var numerator = (sharedForms - 3) * Math.Pow(instanceCount - 1, 1.5);
        var denominator = variablePoints + 1;

        return numerator / denominator;
    }

    /// <summary>
    /// Computes net extraction pressure for a cluster:
    /// max(0, D_before - D_after - H)
    /// where D_after = 0, H = F*0.5 + V*0.3
    /// </summary>
    public static double ComputeExtractionPressure(int sharedForms, int instanceCount, int variablePoints)
    {
        var dBefore = ComputeDefore(sharedForms, instanceCount, variablePoints);
        var helperCost = (sharedForms * HelperFormCost) + (variablePoints * HelperVariableCost);

        return Math.Max(0.0, dBefore - helperCost);
    }

    /// <summary>
    /// Computes file-level extraction pressure from duplication results.
    /// Sums pressure across harmful clusters minus matrix credit (1.5 per case-matrix cluster).
    /// </summary>
    public static FilePressure ComputeFilePressure(DuplicationResults results)
    {
        var clusterPressures = new List<double>();

        foreach (var cluster in results.HarmfulDuplication)
        {
            var pressure = ComputeExtractionPressure(
                cluster.SharedForms,
                cluster.InstanceCount,
                cluster.VariablePoints);
            clusterPressures.Add(pressure);
        }

        var totalPressure = clusterPressures.Sum();
        var matrixCredit = results.CaseMatrixRepetition.Count * MatrixCreditPerCluster;
        var netPressure = Math.Max(0.0, totalPressure - matrixCredit);

        return new FilePressure(
            TotalExtractionPressure: totalPressure,
            ClusterPressures: clusterPressures,
            MatrixCredit: matrixCredit,
            NetPressure: netPressure);
    }
}

using AAFSS.Core.Models;

namespace AAFSS.Infrastructure.Python;

/// <summary>
/// Bridge to Python scipy.stats for statistical distribution fitting.
/// Fits distributions (Weibull, Log-Normal, Gumbel, etc.) and performs
/// goodness-of-fit tests (Kolmogorov-Smirnov) and AIC computation.
/// </summary>
public class StatisticalBridge : IDisposable
{
    private readonly PythonScriptExecutor _executor;

    public StatisticalBridge(PythonScriptExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Fits a distribution to data and returns parameters + goodness-of-fit metrics.
    /// </summary>
    public async Task<(double[] Parameters, double KsStatistic, double KsPValue, double Aic)> FitDistributionAsync(
        double[] data, DistributionType distributionType)
    {
        return await Task.Run(() =>
        {
            dynamic scipy_stats = _executor.ImportModule("scipy.stats");
            dynamic np = _executor.ImportModule("numpy");

            var npData = _executor.ToNumPyArray(data);
            var n = data.Length;

            var distName = GetScipyDistName(distributionType);
            dynamic dist = scipy_stats.GetAttr(distName);

            // Fit distribution
            dynamic params_ = dist.fit(npData);
            var paramList = new List<double>();
            foreach (var p in params_)
                paramList.Add((double)p);

            // KS test
            dynamic ksResult = scipy_stats.kstest(npData, distName, args: params_);
            double ksStat = (double)ksResult[0];
            double ksPVal = (double)ksResult[1];

            // AIC = 2k - 2*log(L), where k = number of parameters
            dynamic logLik = dist.logpdf(npData, params_);
            // Sum log-likelihood values
            var logLikSum = 0.0;
            foreach (var ll in logLik)
                logLikSum += (double)ll;
            var aic = 2.0 * paramList.Count - 2.0 * logLikSum;

            return (paramList.ToArray(), ksStat, ksPVal, aic);
        });
    }

    /// <summary>
    /// Fits all supported distributions and returns the best one (lowest AIC).
    /// </summary>
    public async Task<(DistributionType BestDistribution, double[] Parameters, double KsStatistic, double KsPValue, double Aic)> FitBestDistributionAsync(double[] data)
    {
        var distributions = Enum.GetValues<DistributionType>();
        DistributionType bestDist = DistributionType.Normal;
        double[] bestParams = Array.Empty<double>();
        double bestAic = double.MaxValue;
        double bestKs = 0, bestPVal = 0;

        foreach (var dist in distributions)
        {
            try
            {
                var (params_, ks, pval, aic) = await FitDistributionAsync(data, dist);
                if (aic < bestAic)
                {
                    bestAic = aic;
                    bestDist = dist;
                    bestParams = params_;
                    bestKs = ks;
                    bestPVal = pval;
                }
            }
            catch
            {
                // Skip distributions that fail to fit
                continue;
            }
        }

        return (bestDist, bestParams, bestKs, bestPVal, bestAic);
    }

    /// <summary>
    /// Generates random samples from a fitted distribution.
    /// </summary>
    public async Task<double[]> GenerateSamplesAsync(DistributionType distributionType, double[] parameters, int count)
    {
        return await Task.Run(() =>
        {
            dynamic scipy_stats = _executor.ImportModule("scipy.stats");
            var distName = GetScipyDistName(distributionType);
            dynamic dist = scipy_stats.GetAttr(distName);

            dynamic paramList = new global::Python.Runtime.PyList();
            foreach (var p in parameters)
                paramList.append(new global::Python.Runtime.PyFloat(p));

            dynamic samples = dist.rvs(paramList, size: count);
            var result = new List<double>();
            foreach (var s in samples)
                result.Add((double)s);
            return result.ToArray();
        });
    }

    /// <summary>
    /// Computes the tolerance limit (upper bound for given confidence/coverage).
    /// </summary>
    public Task<double> ComputeToleranceLimitAsync(
        DistributionType distributionType, double[] parameters,
        double confidence = 0.95, double coverage = 0.95)
    {
        return Task.Run(() =>
        {
            dynamic scipy_stats = _executor.ImportModule("scipy.stats");
            var distName = GetScipyDistName(distributionType);
            dynamic dist = scipy_stats.GetAttr(distName);

            // ppf gives the value at the given cumulative probability
            dynamic limit = dist.ppf(coverage, new global::Python.Runtime.PyList());
            return (double)limit;
        });
    }

    private static string GetScipyDistName(DistributionType type) => type switch
    {
        DistributionType.Normal => "norm",
        DistributionType.LogNormal => "lognorm",
        DistributionType.Weibull2P => "weibull_min",
        DistributionType.Weibull3P => "weibull_min",
        DistributionType.Gumbel => "gumbel_r",
        DistributionType.GeneralizedExtremeValue => "genextreme",
        DistributionType.Exponential => "expon",
        DistributionType.Rayleigh => "rayleigh",
        DistributionType.Gamma => "gamma",
        _ => "norm"
    };

    public void Dispose() { }
}

"""
Statistical distribution fitting for rainflow cycle data used in acoustic fatigue analysis.
Supports Normal, Log-Normal, 2-parameter Weibull, 3-parameter Weibull, Gumbel, GEV,
Exponential, Rayleigh, and Gamma distributions.
Performs goodness-of-fit assessment using K-S test and AIC.
"""
import numpy as np
from scipy import stats


def fit_normal(data):
    """
    Fit a Normal (Gaussian) distribution.

    Args:
        data: 1D numpy array of values to fit

    Returns:
        dict: mu, sigma, ks_statistic, ks_pvalue, aic, log_likelihood
    """
    data = np.asarray(data, dtype=np.float64)
    mu, sigma = stats.norm.fit(data)
    ks_stat, ks_pval = stats.kstest(data, 'norm', args=(mu, sigma))
    ll = np.sum(stats.norm.logpdf(data, mu, sigma))
    aic = 4 - 2 * ll  # 2 parameters

    return {
        'distribution': 'normal',
        'parameters': [mu, sigma],
        'param_names': ['mu', 'sigma'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_lognormal(data):
    """
    Fit a Log-Normal distribution (fit to log-transformed data).

    Args:
        data: 1D numpy array of positive values

    Returns:
        dict: shape, scale, loc, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    positive_data = data[data > 0]
    if len(positive_data) < 3:
        return _empty_result('lognormal')

    shape, loc, scale = stats.lognorm.fit(positive_data, floc=0)
    ks_stat, ks_pval = stats.kstest(positive_data, 'lognorm', args=(shape, loc, scale))
    ll = np.sum(stats.lognorm.logpdf(positive_data, shape, loc, scale))
    aic = 4 - 2 * ll  # 2 params (loc fixed at 0)

    return {
        'distribution': 'lognormal',
        'parameters': [shape, loc, scale],
        'param_names': ['shape', 'loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_weibull_2p(data):
    """
    Fit a 2-parameter Weibull distribution (shape, scale).

    Args:
        data: 1D numpy array of positive values

    Returns:
        dict: shape, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    positive_data = data[data > 0]
    if len(positive_data) < 3:
        return _empty_result('weibull2p')

    shape, loc, scale = stats.weibull_min.fit(positive_data, floc=0)
    ks_stat, ks_pval = stats.kstest(positive_data, 'weibull_min', args=(shape, loc, scale))
    ll = np.sum(stats.weibull_min.logpdf(positive_data, shape, loc, scale))
    aic = 4 - 2 * ll  # 2 params (loc fixed at 0)

    return {
        'distribution': 'weibull_2p',
        'parameters': [shape, scale],
        'param_names': ['shape', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_weibull_3p(data):
    """
    Fit a 3-parameter Weibull distribution (shape, loc, scale).

    Args:
        data: 1D numpy array

    Returns:
        dict: shape, loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    if len(data) < 3:
        return _empty_result('weibull3p')

    try:
        shape, loc, scale = stats.weibull_min.fit(data)
        ks_stat, ks_pval = stats.kstest(data, 'weibull_min', args=(shape, loc, scale))
        ll = np.sum(stats.weibull_min.logpdf(data, shape, loc, scale))
        aic = 6 - 2 * ll  # 3 parameters
    except Exception:
        return _empty_result('weibull3p')

    return {
        'distribution': 'weibull_3p',
        'parameters': [shape, loc, scale],
        'param_names': ['shape', 'loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_gumbel(data):
    """
    Fit a Gumbel (Type I extreme value) distribution.

    Args:
        data: 1D numpy array

    Returns:
        dict: loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    if len(data) < 3:
        return _empty_result('gumbel')

    loc, scale = stats.gumbel_r.fit(data)
    ks_stat, ks_pval = stats.kstest(data, 'gumbel_r', args=(loc, scale))
    ll = np.sum(stats.gumbel_r.logpdf(data, loc, scale))
    aic = 4 - 2 * ll  # 2 parameters

    return {
        'distribution': 'gumbel',
        'parameters': [loc, scale],
        'param_names': ['loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_gev(data):
    """
    Fit a Generalized Extreme Value distribution.

    Args:
        data: 1D numpy array

    Returns:
        dict: shape, loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    if len(data) < 5:
        return _empty_result('gev')

    try:
        shape, loc, scale = stats.genextreme.fit(data)
        ks_stat, ks_pval = stats.kstest(data, 'genextreme', args=(shape, loc, scale))
        ll = np.sum(stats.genextreme.logpdf(data, shape, loc, scale))
        aic = 6 - 2 * ll
    except Exception:
        return _empty_result('gev')

    return {
        'distribution': 'gev',
        'parameters': [shape, loc, scale],
        'param_names': ['shape', 'loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_exponential(data):
    """
    Fit an Exponential distribution.

    Args:
        data: 1D numpy array of positive values

    Returns:
        dict: loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    positive_data = data[data > 0]
    if len(positive_data) < 3:
        return _empty_result('exponential')

    loc, scale = stats.expon.fit(positive_data)
    ks_stat, ks_pval = stats.kstest(positive_data, 'expon', args=(loc, scale))
    ll = np.sum(stats.expon.logpdf(positive_data, loc, scale))
    aic = 4 - 2 * ll

    return {
        'distribution': 'exponential',
        'parameters': [loc, scale],
        'param_names': ['loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_rayleigh(data):
    """
    Fit a Rayleigh distribution.

    Args:
        data: 1D numpy array of positive values

    Returns:
        dict: loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    positive_data = data[data > 0]
    if len(positive_data) < 3:
        return _empty_result('rayleigh')

    loc, scale = stats.rayleigh.fit(positive_data)
    ks_stat, ks_pval = stats.kstest(positive_data, 'rayleigh', args=(loc, scale))
    ll = np.sum(stats.rayleigh.logpdf(positive_data, loc, scale))
    aic = 4 - 2 * ll

    return {
        'distribution': 'rayleigh',
        'parameters': [loc, scale],
        'param_names': ['loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def fit_gamma(data):
    """
    Fit a Gamma distribution.

    Args:
        data: 1D numpy array of positive values

    Returns:
        dict: shape, loc, scale, ks_statistic, ks_pvalue, aic
    """
    data = np.asarray(data, dtype=np.float64)
    positive_data = data[data > 0]
    if len(positive_data) < 3:
        return _empty_result('gamma')

    shape, loc, scale = stats.gamma.fit(positive_data)
    ks_stat, ks_pval = stats.kstest(positive_data, 'gamma', args=(shape, loc, scale))
    ll = np.sum(stats.gamma.logpdf(positive_data, shape, loc, scale))
    aic = 6 - 2 * ll  # 3 parameters

    return {
        'distribution': 'gamma',
        'parameters': [shape, loc, scale],
        'param_names': ['shape', 'loc', 'scale'],
        'ks_statistic': float(ks_stat),
        'ks_pvalue': float(ks_pval),
        'aic': float(aic),
        'log_likelihood': float(ll)
    }


def _empty_result(dist_name):
    """Return an empty/placeholder result for failed fits."""
    return {
        'distribution': dist_name,
        'parameters': [],
        'param_names': [],
        'ks_statistic': 1.0,
        'ks_pvalue': 0.0,
        'aic': float('inf'),
        'log_likelihood': float('-inf')
    }


def compute_goodness_of_fit(ks_pvalue):
    """
    Convert K-S p-value to a goodness-of-fit score from 0 to 1.

    Args:
        ks_pvalue: K-S test p-value

    Returns:
        float: Goodness score (1.0 = excellent, 0.0 = poor)
    """
    if ks_pvalue > 0.5:
        return 1.0
    elif ks_pvalue > 0.1:
        return 0.5 + 0.5 * (ks_pvalue - 0.1) / 0.4
    elif ks_pvalue > 0.05:
        return 0.25 + 0.25 * (ks_pvalue - 0.05) / 0.05
    elif ks_pvalue > 0.01:
        return 0.1 + 0.15 * (ks_pvalue - 0.01) / 0.04
    else:
        return max(0.0, 0.1 * ks_pvalue / 0.01)


FITTERS = {
    'normal': fit_normal,
    'lognormal': fit_lognormal,
    'weibull_2p': fit_weibull_2p,
    'weibull_3p': fit_weibull_3p,
    'gumbel': fit_gumbel,
    'gev': fit_gev,
    'exponential': fit_exponential,
    'rayleigh': fit_rayleigh,
    'gamma': fit_gamma,
}


def fit_distribution(data, distribution_type):
    """
    Fit a single specified distribution to the data.

    Args:
        data: 1D numpy array or list of values
        distribution_type: string name of distribution

    Returns:
        dict with fit results including goodness_of_fit score
    """
    data_arr = np.asarray(data, dtype=np.float64)

    if distribution_type not in FITTERS:
        raise ValueError(f"Unknown distribution: {distribution_type}. "
                         f"Available: {list(FITTERS.keys())}")

    result = FITTERS[distribution_type](data_arr)
    result['goodness_of_fit'] = compute_goodness_of_fit(result['ks_pvalue'])
    return result


def fit_best_distribution(data, distributions=None):
    """
    Fit all specified (or all available) distributions and select the best one
    based on lowest AIC score.

    Args:
        data: 1D numpy array or list of values
        distributions: Optional list of distribution names to try (default: all)

    Returns:
        dict with keys: best_fit (the winning distribution result), all_fits (list of all results)
    """
    data_arr = np.asarray(data, dtype=np.float64)

    if distributions is None:
        distributions = list(FITTERS.keys())

    all_fits = []
    for dist_name in distributions:
        try:
            result = FITTERS[dist_name](data_arr)
            result['goodness_of_fit'] = compute_goodness_of_fit(result['ks_pvalue'])
            all_fits.append(result)
        except Exception:
            continue

    if not all_fits:
        return {'best_fit': None, 'all_fits': [], 'error': 'All distribution fits failed'}

    # Select best by AIC (lower is better)
    best = min(all_fits, key=lambda r: r.get('aic', float('inf')))

    return {
        'best_fit': best,
        'all_fits': all_fits,
        'num_fitted': len(all_fits),
        'num_requested': len(distributions)
    }


def generate_samples(distribution_type, parameters, n_samples=1000):
    """
    Generate random samples from a fitted distribution.

    Args:
        distribution_type: string name of distribution
        parameters: list of distribution parameters
        n_samples: number of samples to generate

    Returns:
        list of generated samples
    """
    data_arr = np.asarray(data, dtype=np.float64)

    if distribution_type not in FITTERS:
        raise ValueError(f"Unknown distribution: {distribution_type}. "
                         f"Available: {list(FITTERS.keys())}")

    result = FITTERS[distribution_type](data_arr)
    result['goodness_of_fit'] = compute_goodness_of_fit(result['ks_pvalue'])
    return result


def compute_tolerance_limit(data, distribution_type, confidence=0.95, coverage=0.95):
    """
    Compute the upper tolerance limit (B-basis or A-basis) for a fitted distribution.

    For normal distribution, uses the k-factor approach.
    For Weibull, uses the probability-based approach.

    Args:
        data: 1D numpy array of values
        distribution_type: distribution name for fitting
        confidence: Confidence level (default 0.95)
        coverage: Coverage proportion (default 0.95)

    Returns:
        tuple: (tolerance_limit, k_factor)
    """
    data_arr = np.asarray(data, dtype=np.float64)
    n = len(data_arr)

    if distribution_type == 'normal':
        mu, sigma = stats.norm.fit(data_arr)
        # k-factor for normal distribution
        z_p = stats.norm.ppf(coverage)
        chi2 = stats.chi2.ppf(1 - confidence, n - 1)
        k = z_p * np.sqrt(n) / np.sqrt(chi2)
        limit = mu + k * sigma
        return float(limit), float(k)
    else:
        # Non-parametric approach: use order statistics
        sorted_data = np.sort(data_arr)
        # Use Wilks' method for non-parametric tolerance limit
        r = int(n * coverage)
        if r >= n:
            r = n - 1
        limit = sorted_data[r]
        return float(limit), 0.0

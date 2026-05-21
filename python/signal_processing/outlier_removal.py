"""
Outlier detection and removal for acoustic/vibration time series data.
Supports Grubbs' test, 3-sigma rule, IQR method, and Hampel filter.
"""
import numpy as np
from scipy import stats


def grubbs_test(data, alpha=0.05):
    """
    Iterative Grubbs' test for outlier detection.
    Removes outliers one at a time until no more are found.

    Args:
        data: 1D numpy array
        alpha: Significance level (default 0.05)

    Returns:
        tuple: (cleaned_data, outlier_indices, outlier_count)
    """
    data = np.asarray(data, dtype=np.float64).copy()
    indices = np.arange(len(data))
    outliers = []

    while True:
        n = len(data)
        if n < 3:
            break

        mean = np.mean(data)
        std = np.std(data, ddof=1)
        if std == 0:
            break

        abs_dev = np.abs(data - mean)
        max_idx = np.argmax(abs_dev)
        g = abs_dev[max_idx] / std

        # Critical value for Grubbs' test
        t = stats.t.ppf(1 - alpha / (2 * n), n - 2)
        g_crit = ((n - 1) / np.sqrt(n)) * np.sqrt(t**2 / (n - 2 + t**2))

        if g > g_crit:
            outliers.append(indices[max_idx])
            data = np.delete(data, max_idx)
            indices = np.delete(indices, max_idx)
        else:
            break

    outlier_mask = np.zeros(len(data) + len(outliers), dtype=bool)
    for idx in outliers:
        outlier_mask[idx] = True

    return data, np.array(outliers, dtype=int), len(outliers)


def three_sigma_removal(data, n_sigma=3.0):
    """
    Remove outliers beyond n standard deviations from the mean.

    Args:
        data: 1D numpy array
        n_sigma: Number of standard deviations for threshold (default 3.0)

    Returns:
        tuple: (cleaned_data, outlier_indices, outlier_count, [lower_bound, upper_bound])
    """
    data = np.asarray(data, dtype=np.float64)
    mean = np.mean(data)
    std = np.std(data, ddof=1)

    if std == 0:
        return data, np.array([], dtype=int), 0, [mean, mean]

    lower = mean - n_sigma * std
    upper = mean + n_sigma * std

    inlier_mask = (data >= lower) & (data <= upper)
    outlier_indices = np.where(~inlier_mask)[0]
    cleaned = data[inlier_mask]

    return cleaned, outlier_indices, len(outlier_indices), [lower, upper]


def iqr_removal(data, factor=1.5):
    """
    Remove outliers using the Interquartile Range (IQR) method.

    Args:
        data: 1D numpy array
        factor: IQR multiplier (default 1.5, use 3.0 for extreme outliers only)

    Returns:
        tuple: (cleaned_data, outlier_indices, outlier_count, [lower, upper])
    """
    data = np.asarray(data, dtype=np.float64)
    q1 = np.percentile(data, 25)
    q3 = np.percentile(data, 75)
    iqr = q3 - q1

    lower = q1 - factor * iqr
    upper = q3 + factor * iqr

    inlier_mask = (data >= lower) & (data <= upper)
    outlier_indices = np.where(~inlier_mask)[0]
    cleaned = data[inlier_mask]

    return cleaned, outlier_indices, len(outlier_indices), [lower, upper]


def hampel_filter(data, window_size=5, n_sigma=3.0):
    """
    Hampel filter: replace outliers with local median.
    Useful for spike removal in acoustic signals.

    Args:
        data: 1D numpy array
        window_size: Sliding window half-width (default 5)
        n_sigma: Threshold multiplier for MAD (default 3.0)

    Returns:
        tuple: (filtered_data, outlier_indices, outlier_count)
    """
    data = np.asarray(data, dtype=np.float64).copy()
    n = len(data)
    if n < 2 * window_size + 1:
        return data, np.array([], dtype=int), 0

    k = 1.4826  # Scale factor for MAD to approximate standard deviation
    outlier_indices = []

    for i in range(window_size, n - window_size):
        window = data[i - window_size:i + window_size + 1]
        median = np.median(window)
        mad = k * np.median(np.abs(window - median))

        if mad == 0:
            continue

        if np.abs(data[i] - median) > n_sigma * mad:
            outlier_indices.append(i)
            data[i] = median

    return data, np.array(outlier_indices, dtype=int), len(outlier_indices)


def remove_outliers(data, method='three_sigma', **kwargs):
    """
    Unified outlier removal interface.

    Args:
        data: 1D numpy array or list
        method: 'three_sigma', 'grubbs', 'iqr', or 'hampel'
        **kwargs: Method-specific parameters

    Returns:
        dict with keys: cleaned_data, outlier_indices, outlier_count, bounds, method
    """
    data_arr = np.asarray(data, dtype=np.float64)

    if method == 'three_sigma':
        n_sigma = float(kwargs.get('n_sigma', 3.0))
        cleaned, indices, count, bounds = three_sigma_removal(data_arr, n_sigma)
    elif method == 'grubbs':
        alpha = float(kwargs.get('alpha', 0.05))
        cleaned, indices, count = grubbs_test(data_arr, alpha)
        bounds = [0, 0]
    elif method == 'iqr':
        factor = float(kwargs.get('factor', 1.5))
        cleaned, indices, count, bounds = iqr_removal(data_arr, factor)
    elif method == 'hampel':
        window = int(kwargs.get('window_size', 5))
        n_sigma = float(kwargs.get('n_sigma', 3.0))
        cleaned, indices, count = hampel_filter(data_arr, window, n_sigma)
        bounds = [0, 0]
    else:
        raise ValueError(f"Unknown outlier removal method: {method}")

    return {
        'cleaned_data': cleaned.tolist(),
        'outlier_indices': indices.tolist(),
        'outlier_count': count,
        'bounds': bounds,
        'method': method,
        'original_length': len(data_arr),
        'cleaned_length': len(cleaned)
    }

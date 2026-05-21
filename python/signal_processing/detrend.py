"""
Detrending operations for acoustic/vibration time series data.
Supports linear, polynomial, and moving-average detrending.
"""
import numpy as np
from scipy import signal


def linear_detrend(data):
    """
    Remove the best-fit linear trend from the data.

    Args:
        data: 1D numpy array

    Returns:
        tuple: (detrended_data, slope, intercept, r_squared)
    """
    data = np.asarray(data, dtype=np.float64)
    n = len(data)
    x = np.arange(n, dtype=np.float64)

    # Linear regression
    x_mean = np.mean(x)
    y_mean = np.mean(data)
    slope = np.sum((x - x_mean) * (data - y_mean)) / np.sum((x - x_mean) ** 2)
    intercept = y_mean - slope * x_mean

    trend = slope * x + intercept
    detrended = data - trend

    # R-squared
    ss_res = np.sum((data - trend) ** 2)
    ss_tot = np.sum((data - y_mean) ** 2)
    r_squared = 1 - ss_res / ss_tot if ss_tot != 0 else 0

    return detrended, slope, intercept, r_squared


def polynomial_detrend(data, order=2):
    """
    Remove a polynomial trend of specified order.

    Args:
        data: 1D numpy array
        order: Polynomial order (default 2 for quadratic)

    Returns:
        tuple: (detrended_data, coefficients)
    """
    data = np.asarray(data, dtype=np.float64)
    n = len(data)
    x = np.arange(n, dtype=np.float64)

    coeffs = np.polyfit(x, data, order)
    trend = np.polyval(coeffs, x)
    detrended = data - trend

    return detrended, coeffs.tolist()


def moving_average_detrend(data, window_size=100):
    """
    Remove a moving average trend from the data.
    Useful for removing slow-varying DC offsets.

    Args:
        data: 1D numpy array
        window_size: Moving average window size

    Returns:
        tuple: (detrended_data, trend)
    """
    data = np.asarray(data, dtype=np.float64)
    n = len(data)

    if window_size >= n:
        window_size = max(1, n // 4)

    # Moving average with padding
    kernel = np.ones(window_size) / window_size
    trend = np.convolve(data, kernel, mode='same')

    detrended = data - trend

    return detrended, trend.tolist()


def mean_removal(data):
    """
    Remove DC offset (mean) from the data.

    Args:
        data: 1D numpy array

    Returns:
        tuple: (centered_data, mean_value)
    """
    data = np.asarray(data, dtype=np.float64)
    mean_val = np.mean(data)
    return data - mean_val, mean_val


def detrend(data, method='linear', **kwargs):
    """
    Unified detrending interface.

    Args:
        data: 1D numpy array or list
        method: 'linear', 'polynomial', 'moving_average', or 'mean'
        **kwargs: Method-specific parameters

    Returns:
        dict with keys: detrended_data, method, trend_info
    """
    data_arr = np.asarray(data, dtype=np.float64)

    if method == 'linear':
        detrended, slope, intercept, r2 = linear_detrend(data_arr)
        return {
            'detrended_data': detrended.tolist(),
            'method': 'linear',
            'slope': slope,
            'intercept': intercept,
            'r_squared': r2
        }
    elif method == 'polynomial':
        order = int(kwargs.get('order', 2))
        detrended, coeffs = polynomial_detrend(data_arr, order)
        return {
            'detrended_data': detrended.tolist(),
            'method': 'polynomial',
            'order': order,
            'coefficients': coeffs
        }
    elif method == 'moving_average':
        window = int(kwargs.get('window_size', 100))
        detrended, trend = moving_average_detrend(data_arr, window)
        return {
            'detrended_data': detrended.tolist(),
            'method': 'moving_average',
            'window_size': window,
            'trend': trend
        }
    elif method == 'mean':
        detrended, mean_val = mean_removal(data_arr)
        return {
            'detrended_data': detrended.tolist(),
            'method': 'mean',
            'mean_value': mean_val
        }
    else:
        raise ValueError(f"Unknown detrend method: {method}")

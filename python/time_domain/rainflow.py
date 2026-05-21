"""
ASTM E1049-85 Standard rainflow cycle counting implementation.
Four-point algorithm for extracting fatigue cycles from irregular stress/strain histories.
"""
import numpy as np


def rainflow_counting(data):
    """
    Perform ASTM E1049-85 standard rainflow cycle counting (4-point algorithm).

    Implementation follows the ASTM standard with the standard four-point rule:
    A cycle is extracted when |Si+2 - Si+1| >= |Si+1 - Si| (inner range exceeds outer range).

    Args:
        data: 1D numpy array of time series data (stress, strain, or acoustic pressure)

    Returns:
        dict with keys:
            from_values: list of cycle start values
            to_values: list of cycle end values
            amplitudes: list of cycle amplitudes (half of range)
            mean_values: list of cycle mean values
            ranges: list of cycle ranges (peak-to-peak)
            cycles: total number of extracted cycles
            residuals: remaining turning points after rainflow extraction
            max_amplitude: maximum cycle amplitude
    """
    data = np.asarray(data, dtype=np.float64)

    # Step 1: Extract turning points (peaks and valleys)
    turning_points = extract_turning_points(data)

    if len(turning_points) < 3:
        return {
            'from_values': [],
            'to_values': [],
            'amplitudes': [],
            'mean_values': [],
            'ranges': [],
            'cycles': 0,
            'residuals': turning_points.tolist(),
            'max_amplitude': 0.0
        }

    # Step 2: Apply four-point rainflow counting
    from_vals = []
    to_vals = []
    amplitudes = []
    means = []
    ranges = []

    # Work with a mutable list
    points = turning_points.tolist()
    i = 0

    while len(points) >= 3:
        if i + 2 >= len(points):
            break

        s0 = points[i]
        s1 = points[i + 1]
        s2 = points[i + 2]

        range_inner = abs(s1 - s0)
        range_outer = abs(s2 - s1)

        if range_outer >= range_inner:
            # Extract cycle from s0 to s1
            from_val = s0
            to_val = s1
            amplitude = range_inner / 2.0
            mean = (s0 + s1) / 2.0

            from_vals.append(from_val)
            to_vals.append(to_val)
            amplitudes.append(amplitude)
            means.append(mean)
            ranges.append(range_inner)

            # Remove s0 and s1 from the sequence
            points.pop(i)
            points.pop(i)

            # Go back two steps to recheck
            i = max(0, i - 2)
        else:
            i += 1

    # Remaining points form the residual sequence
    residuals = points

    return {
        'from_values': from_vals,
        'to_values': to_vals,
        'amplitudes': amplitudes,
        'mean_values': means,
        'ranges': ranges,
        'cycles': len(from_vals),
        'residuals': residuals,
        'max_amplitude': max(amplitudes) if amplitudes else 0.0,
        'max_range': max(ranges) if ranges else 0.0
    }


def extract_turning_points(data):
    """
    Extract turning points (local maxima and minima) from a time series.

    A point is a turning point if the sign of the slope changes.

    Args:
        data: 1D numpy array

    Returns:
        numpy array of turning point values
    """
    data = np.asarray(data, dtype=np.float64)
    n = len(data)

    if n < 3:
        return data.copy()

    # Compute differences (slope sign)
    diffs = np.diff(data)
    signs = np.sign(diffs)

    turning_indices = [0]  # Always include the first point

    for i in range(1, n - 1):
        if signs[i] == 0 and signs[i - 1] == 0:
            # Flat region - skip
            continue
        if signs[i] != signs[i - 1] and signs[i] != 0:
            # Sign change => turning point
            turning_indices.append(i)

    turning_indices.append(n - 1)  # Always include the last point

    return data[np.array(turning_indices)]


def build_from_to_matrix(from_values, to_values, num_bins=64):
    """
    Build a from-to cycle count matrix from rainflow results.

    Args:
        from_values: List of cycle starting values
        to_values: List of cycle ending values
        num_bins: Number of bins (default 64 => 64x64 matrix)

    Returns:
        tuple: (from_to_matrix, bin_edges)
    """
    from_vals = np.asarray(from_values)
    to_vals = np.asarray(to_values)

    if len(from_vals) == 0:
        return np.zeros((num_bins, num_bins)), np.linspace(-1, 1, num_bins + 1)

    all_vals = np.concatenate([from_vals, to_vals])
    v_min, v_max = all_vals.min(), all_vals.max()

    if np.isclose(v_min, v_max):
        v_min -= 1.0
        v_max += 1.0

    bin_edges = np.linspace(v_min, v_max, num_bins + 1)
    matrix = np.zeros((num_bins, num_bins), dtype=int)

    for f, t in zip(from_vals, to_vals):
        i = np.digitize(f, bin_edges) - 1
        j = np.digitize(t, bin_edges) - 1
        i = np.clip(i, 0, num_bins - 1)
        j = np.clip(j, 0, num_bins - 1)
        matrix[i, j] += 1

    return matrix, bin_edges


def build_mean_amplitude_matrix(amplitudes, means, num_bins=64):
    """
    Build a mean-vs-amplitude cycle count matrix.

    Args:
        amplitudes: List of cycle amplitudes
        means: List of cycle mean values
        num_bins: Number of bins

    Returns:
        tuple: (mean_amplitude_matrix, amp_edges, mean_edges)
    """
    amps = np.asarray(amplitudes)
    mn = np.asarray(means)

    if len(amps) == 0:
        return np.zeros((num_bins, num_bins)), np.linspace(0, 1, num_bins + 1), np.linspace(-1, 1, num_bins + 1)

    amp_edges = np.linspace(0, amps.max() * 1.01, num_bins + 1)
    mean_edges = np.linspace(mn.min(), mn.max(), num_bins + 1)

    matrix = np.zeros((num_bins, num_bins), dtype=int)

    for a, m in zip(amps, mn):
        i = np.digitize(a, amp_edges) - 1
        j = np.digitize(m, mean_edges) - 1
        i = np.clip(i, 0, num_bins - 1)
        j = np.clip(j, 0, num_bins - 1)
        matrix[i, j] += 1

    return matrix, amp_edges, mean_edges


def rainflow_full(data, num_bins=64):
    """
    Full rainflow counting with matrix generation.

    Args:
        data: 1D numpy array of time series data
        num_bins: Number of bins for discretization matrices

    Returns:
        dict with all rainflow results including from-to and mean-amplitude matrices
    """
    result = rainflow_counting(data)

    if result['cycles'] > 0:
        from_to_matrix, bin_edges = build_from_to_matrix(
            result['from_values'], result['to_values'], num_bins)
        mean_amp_matrix, amp_edges, mean_edges = build_mean_amplitude_matrix(
            result['amplitudes'], result['mean_values'], num_bins)

        result['from_to_matrix'] = from_to_matrix.flatten().tolist()
        result['from_to_matrix_shape'] = [num_bins, num_bins]
        result['mean_amplitude_matrix'] = mean_amp_matrix.flatten().tolist()
        result['mean_amplitude_matrix_shape'] = [num_bins, num_bins]
        result['bin_count'] = num_bins
        result['bin_edges'] = bin_edges.tolist()
    else:
        result['from_to_matrix'] = np.zeros((num_bins, num_bins)).flatten().tolist()
        result['mean_amplitude_matrix'] = np.zeros((num_bins, num_bins)).flatten().tolist()
        result['bin_count'] = num_bins

    return result

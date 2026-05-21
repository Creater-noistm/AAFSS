"""
Octave band spectrum analysis per ISO 266 / IEC 61260.
Supports 1/1, 1/3, 1/6, and 1/12 octave band analysis using ANSI/IEC standard filters.
"""
import numpy as np
from scipy.signal import butter, sosfilt


def get_octave_center_frequencies(fraction=3, low_freq=10.0, high_freq=20000.0):
    """
    Compute standard octave band center frequencies per ISO 266.

    Args:
        fraction: Octave fraction (1, 3, 6, or 12)
        low_freq: Lower frequency bound in Hz
        high_freq: Upper frequency bound in Hz

    Returns:
        list of center frequencies in Hz
    """
    # Reference frequency: 1000 Hz
    ref = 1000.0

    centers = []
    # Go up from 1000 Hz
    n = 0
    while True:
        f = ref * 10 ** (n / (10 * fraction))
        if f > high_freq * 1.01:
            break
        if f >= low_freq * 0.99:
            centers.append(f)
        n += 1

    # Go down from 1000 Hz
    n = -1
    while True:
        f = ref * 10 ** (n / (10 * fraction))
        if f < low_freq * 0.99:
            break
        if f <= high_freq * 1.01:
            centers.insert(0, f)
        n -= 1

    return [round(f, 4) for f in centers]


def get_octave_band_edges(centers, fraction=3):
    """
    Compute lower and upper band-edge frequencies for each center frequency.

    Args:
        centers: List of center frequencies in Hz
        fraction: Octave fraction

    Returns:
        tuple: (lower_edges, upper_edges) as numpy arrays
    """
    ratio = 10 ** (1 / (2 * fraction))
    centers = np.asarray(centers)
    lower = centers / ratio
    upper = centers * ratio
    return lower, upper


def compute_octave_bands(data, sample_rate, fraction=3, low_freq=10.0, high_freq=20000.0, ref_value=2e-5):
    """
    Compute octave band levels from time series data using digital Butterworth filters.

    The signal is passed through a bank of bandpass filters corresponding to each
    octave band. The RMS of each filtered signal is computed and converted to dB.

    Args:
        data: 1D numpy array of time series data in Pa
        sample_rate: Sampling frequency in Hz
        fraction: Octave fraction (1, 3, 6, or 12; default 3 for 1/3 octave)
        low_freq: Lowest center frequency to compute in Hz
        high_freq: Highest center frequency to compute in Hz
        ref_value: Reference value for dB (default 2e-5 Pa for SPL)

    Returns:
        dict with keys:
            center_frequencies: list of center frequencies in Hz
            band_levels: list of band levels in dB
            band_lower_edges: list of lower band-edge frequencies
            band_upper_edges: list of upper band-edge frequencies
            overall_level: overall SPL in dB
            fraction: octave fraction used
    """
    data = np.asarray(data, dtype=np.float64)
    nyquist = sample_rate / 2.0

    centers = get_octave_center_frequencies(fraction, low_freq, high_freq)
    lower_edges, upper_edges = get_octave_band_edges(centers, fraction)

    band_levels = []
    valid_centers = []
    valid_lower = []
    valid_upper = []

    for fc, fl, fu in zip(centers, lower_edges, upper_edges):
        # Skip bands where upper edge exceeds Nyquist
        if fu >= nyquist * 0.95:
            continue

        # Design Butterworth bandpass filter (3rd order for stability)
        try:
            sos = butter(3, [fl / nyquist, fu / nyquist], btype='bandpass', output='sos')
            filtered = sosfilt(sos, data)

            # Compute RMS in Pa, then convert to dB
            rms = np.sqrt(np.mean(filtered ** 2))
            if rms > 0:
                level_db = 20 * np.log10(rms / ref_value)
            else:
                level_db = -np.inf

            valid_centers.append(fc)
            valid_lower.append(fl)
            valid_upper.append(fu)
            band_levels.append(round(level_db, 3))
        except Exception:
            continue

    # Overall level
    overall_rms = np.sqrt(np.mean(data ** 2))
    overall_level = 20 * np.log10(overall_rms / ref_value) if overall_rms > 0 else -np.inf

    return {
        'center_frequencies': [round(f, 2) for f in valid_centers],
        'band_levels': band_levels,
        'band_lower_edges': [round(f, 2) for f in valid_lower],
        'band_upper_edges': [round(f, 2) for f in valid_upper],
        'overall_level': round(overall_level, 3),
        'fraction': fraction,
        'num_bands': len(valid_centers)
    }


def compute_octave_from_psd(frequencies, psd, fraction=3, low_freq=10.0, high_freq=20000.0, ref_value=2e-5):
    """
    Compute octave band levels from an existing PSD estimate (no time data needed).
    Integrates the PSD across each band to compute band power.

    Args:
        frequencies: 1D numpy array of PSD frequency bins in Hz
        psd: 1D numpy array of PSD values in Pa^2/Hz
        fraction: Octave fraction (default 3)
        low_freq: Lowest frequency in Hz
        high_freq: Highest frequency in Hz
        ref_value: Reference for dB (default 2e-5 Pa for SPL)

    Returns:
        dict similar to compute_octave_bands
    """
    frequencies = np.asarray(frequencies, dtype=np.float64)
    psd = np.asarray(psd, dtype=np.float64)

    centers = get_octave_center_frequencies(fraction, low_freq, high_freq)
    lower_edges, upper_edges = get_octave_band_edges(centers, fraction)

    # Frequency resolution
    if len(frequencies) > 1:
        df = frequencies[1] - frequencies[0]
    else:
        df = 1.0

    band_levels = []
    valid_centers = []

    for fc, fl, fu in zip(centers, lower_edges, upper_edges):
        mask = (frequencies >= fl) & (frequencies < fu)
        if np.any(mask):
            band_power = np.sum(psd[mask]) * df
            if band_power > 0:
                level_db = 10 * np.log10(band_power / (ref_value ** 2))
            else:
                level_db = -np.inf
            valid_centers.append(fc)
            band_levels.append(round(level_db, 3))

    return {
        'center_frequencies': [round(f, 2) for f in valid_centers],
        'band_levels': band_levels,
        'fraction': fraction,
        'num_bands': len(valid_centers)
    }

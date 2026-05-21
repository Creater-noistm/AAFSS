"""
Power Spectral Density estimation using Welch's method.
Provides PSD computation with configurable window, overlap, and averaging.
"""
import numpy as np
from scipy.signal import welch, get_window, csd, coherence


def compute_psd(data, sample_rate, nperseg=4096, window='hann', overlap=0.5, detrend='constant'):
    """
    Compute Power Spectral Density using Welch's averaged periodogram method.

    Args:
        data: 1D numpy array of time series data
        sample_rate: Sampling frequency in Hz
        nperseg: Length of each segment (FFT size)
        window: Window function name ('hann', 'hamming', 'blackman', 'flattop')
        overlap: Overlap ratio (0 to 1, default 0.5)
        detrend: 'constant' (remove mean) or 'linear' or False

    Returns:
        dict with keys:
            frequencies: frequency bins in Hz
            psd: power spectral density values (units^2/Hz)
            psd_db: PSD in dB (ref=1 unit^2/Hz)
            sample_rate: sample rate used
            nperseg: segment length used
    """
    data = np.asarray(data, dtype=np.float64)
    noverlap = int(nperseg * overlap)

    frequencies, psd = welch(
        data,
        fs=sample_rate,
        nperseg=nperseg,
        noverlap=noverlap,
        window=window,
        detrend=detrend,
        scaling='density'
    )

    # Convert to dB (ref 1)
    psd_db = 10 * np.log10(np.maximum(psd, 1e-20))

    return {
        'frequencies': frequencies.tolist(),
        'psd': psd.tolist(),
        'psd_db': psd_db.tolist(),
        'sample_rate': sample_rate,
        'nperseg': nperseg,
        'frequency_resolution': frequencies[1] - frequencies[0] if len(frequencies) > 1 else 0
    }


def compute_psd_db_spl(data, sample_rate, nperseg=4096, window='hann', overlap=0.5,
                       ref_value=2e-5):
    """
    Compute PSD in dB SPL (ref 20 uPa).

    Args:
        data: 1D numpy array of sound pressure in Pa
        sample_rate: Sampling frequency in Hz
        nperseg: Segment length
        window: Window function
        overlap: Overlap ratio
        ref_value: Reference value (default 2e-5 Pa)

    Returns:
        dict similar to compute_psd but with SPL scaling
    """
    data = np.asarray(data, dtype=np.float64)
    noverlap = int(nperseg * overlap)

    frequencies, psd = welch(
        data, fs=sample_rate, nperseg=nperseg,
        noverlap=noverlap, window=window, scaling='density'
    )

    # PSD in dB SPL: 10*log10(PSD / ref^2)
    psd_db_spl = 10 * np.log10(np.maximum(psd / (ref_value ** 2), 1e-20))

    return {
        'frequencies': frequencies.tolist(),
        'psd': psd.tolist(),
        'psd_db_spl': psd_db_spl.tolist(),
        'sample_rate': sample_rate,
        'nperseg': nperseg,
        'frequency_resolution': frequencies[1] - frequencies[0] if len(frequencies) > 1 else 0
    }


def compute_cross_spectrum(data1, data2, sample_rate, nperseg=4096, overlap=0.5):
    """
    Compute cross-spectral density between two signals.

    Args:
        data1, data2: 1D numpy arrays of equal length
        sample_rate: Sampling frequency in Hz
        nperseg: Segment length
        overlap: Overlap ratio

    Returns:
        dict with keys: frequencies, cross_psd_real, cross_psd_imag, cross_psd_magnitude, cross_psd_phase
    """
    data1 = np.asarray(data1, dtype=np.float64)
    data2 = np.asarray(data2, dtype=np.float64)
    noverlap = int(nperseg * overlap)

    frequencies, csd_values = csd(
        data1, data2, fs=sample_rate, nperseg=nperseg,
        noverlap=noverlap, window='hann'
    )

    return {
        'frequencies': frequencies.tolist(),
        'cross_psd_real': csd_values.real.tolist(),
        'cross_psd_imag': csd_values.imag.tolist(),
        'cross_psd_magnitude': np.abs(csd_values).tolist(),
        'cross_psd_phase': np.angle(csd_values).tolist(),
        'sample_rate': sample_rate
    }


def compute_coherence(data1, data2, sample_rate, nperseg=4096, overlap=0.5):
    """
    Compute magnitude-squared coherence between two signals.

    Args:
        data1, data2: 1D numpy arrays of equal length
        sample_rate: Sampling frequency in Hz
        nperseg: Segment length
        overlap: Overlap ratio

    Returns:
        dict with keys: frequencies, coherence, sample_rate
    """
    data1 = np.asarray(data1, dtype=np.float64)
    data2 = np.asarray(data2, dtype=np.float64)
    noverlap = int(nperseg * overlap)

    frequencies, coh = coherence(
        data1, data2, fs=sample_rate, nperseg=nperseg,
        noverlap=noverlap, window='hann'
    )

    return {
        'frequencies': frequencies.tolist(),
        'coherence': coh.tolist(),
        'sample_rate': sample_rate
    }


def zoom_fft(data, sample_rate, f_min, f_max, nperseg=4096):
    """
    Perform zoom FFT analysis on a specific frequency range (higher resolution).

    Args:
        data: 1D numpy array
        sample_rate: Sampling frequency in Hz
        f_min: Minimum frequency in Hz
        f_max: Maximum frequency in Hz
        nperseg: Segment length for Welch averaging

    Returns:
        dict with keys: frequencies, psd_db_spl
    """
    data = np.asarray(data, dtype=np.float64)
    noverlap = nperseg // 2

    frequencies, psd = welch(
        data, fs=sample_rate, nperseg=nperseg,
        noverlap=noverlap, window='hann', scaling='density'
    )

    mask = (frequencies >= f_min) & (frequencies <= f_max)
    zoomed_f = frequencies[mask]
    zoomed_psd = psd[mask]

    psd_db = 10 * np.log10(np.maximum(zoomed_psd, 1e-20))

    return {
        'frequencies': zoomed_f.tolist(),
        'psd_db': psd_db.tolist(),
        'f_min': f_min,
        'f_max': f_max,
        'frequency_resolution': zoomed_f[1] - zoomed_f[0] if len(zoomed_f) > 1 else 0
    }


def compute_band_power(frequencies, psd, f_min, f_max):
    """
    Integrate PSD over a frequency band to get band power.

    Args:
        frequencies: Frequency bins
        psd: PSD values
        f_min, f_max: Integration limits

    Returns:
        Band power value
    """
    frequencies = np.asarray(frequencies)
    psd = np.asarray(psd)
    mask = (frequencies >= f_min) & (frequencies <= f_max)
    if not np.any(mask):
        return 0.0
    df = frequencies[1] - frequencies[0] if len(frequencies) > 1 else 1.0
    return np.sum(psd[mask]) * df

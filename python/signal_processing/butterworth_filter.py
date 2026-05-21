"""
Butterworth digital filter implementation for acoustic fatigue signal processing.
Supports lowpass, highpass, bandpass, and bandstop filter types.
"""
import numpy as np
from scipy.signal import butter, sosfiltfilt, freqz


def butterworth_lowpass(data, sample_rate, cutoff, order=4):
    """
    Apply Butterworth lowpass filter using forward-backward filtering (zero-phase).

    Args:
        data: 1D numpy array of input signal
        sample_rate: Sampling frequency in Hz
        cutoff: Cutoff frequency in Hz
        order: Filter order (default 4)

    Returns:
        1D numpy array of filtered signal
    """
    if cutoff <= 0 or cutoff >= sample_rate / 2:
        raise ValueError(f"Cutoff must be in (0, {sample_rate/2}) Hz")

    nyquist = sample_rate / 2.0
    normalized_cutoff = cutoff / nyquist
    sos = butter(order, normalized_cutoff, btype='low', output='sos')
    return sosfiltfilt(sos, data)


def butterworth_highpass(data, sample_rate, cutoff, order=4):
    """
    Apply Butterworth highpass filter using forward-backward filtering.

    Args:
        data: 1D numpy array of input signal
        sample_rate: Sampling frequency in Hz
        cutoff: Cutoff frequency in Hz
        order: Filter order (default 4)

    Returns:
        1D numpy array of filtered signal
    """
    if cutoff <= 0 or cutoff >= sample_rate / 2:
        raise ValueError(f"Cutoff must be in (0, {sample_rate/2}) Hz")

    nyquist = sample_rate / 2.0
    normalized_cutoff = cutoff / nyquist
    sos = butter(order, normalized_cutoff, btype='high', output='sos')
    return sosfiltfilt(sos, data)


def butterworth_bandpass(data, sample_rate, low_cutoff, high_cutoff, order=4):
    """
    Apply Butterworth bandpass filter using forward-backward filtering.

    Args:
        data: 1D numpy array of input signal
        sample_rate: Sampling frequency in Hz
        low_cutoff: Lower cutoff frequency in Hz
        high_cutoff: Upper cutoff frequency in Hz
        order: Filter order (default 4)

    Returns:
        1D numpy array of filtered signal
    """
    nyquist = sample_rate / 2.0
    if low_cutoff <= 0 or high_cutoff >= nyquist or low_cutoff >= high_cutoff:
        raise ValueError(f"Invalid cutoff range. Must satisfy: 0 < low < high < {nyquist}")

    normalized_cutoffs = [low_cutoff / nyquist, high_cutoff / nyquist]
    sos = butter(order, normalized_cutoffs, btype='bandpass', output='sos')
    return sosfiltfilt(sos, data)


def butterworth_bandstop(data, sample_rate, low_cutoff, high_cutoff, order=4):
    """
    Apply Butterworth bandstop (notch) filter.

    Args:
        data: 1D numpy array of input signal
        sample_rate: Sampling frequency in Hz
        low_cutoff: Lower cutoff frequency in Hz
        high_cutoff: Upper cutoff frequency in Hz
        order: Filter order (default 4)

    Returns:
        1D numpy array of filtered signal
    """
    nyquist = sample_rate / 2.0
    normalized_cutoffs = [low_cutoff / nyquist, high_cutoff / nyquist]
    sos = butter(order, normalized_cutoffs, btype='bandstop', output='sos')
    return sosfiltfilt(sos, data)


def get_filter_response(sample_rate, cutoff, order=4, filter_type='low'):
    """
    Compute the frequency response of the Butterworth filter.

    Args:
        sample_rate: Sampling frequency in Hz
        cutoff: Cutoff frequency (for lowpass/highpass) or [low, high] (for bandpass/bandstop)
        order: Filter order
        filter_type: 'low', 'high', 'bandpass', or 'bandstop'

    Returns:
        tuple: (frequencies, magnitude_dB) arrays
    """
    nyquist = sample_rate / 2.0

    if isinstance(cutoff, (list, tuple)):
        normalized = [c / nyquist for c in cutoff]
    else:
        normalized = cutoff / nyquist

    sos = butter(order, normalized, btype=filter_type, output='sos')
    w, h = freqz(sos, worN=512)
    freqs = w * nyquist / np.pi
    mag_db = 20 * np.log10(np.abs(h))

    return freqs, mag_db


def apply_filter(data, sample_rate, filter_type, params):
    """
    Unified filter application interface called by the .NET bridge.

    Args:
        data: 1D numpy array or list of arrays
        sample_rate: Sampling frequency in Hz
        filter_type: 'lowpass', 'highpass', 'bandpass', or 'bandstop'
        params: dict with filter parameters (cutoff, order, low_cutoff, high_cutoff)

    Returns:
        Filtered data in the same shape as input
    """
    data_arr = np.asarray(data, dtype=np.float64)
    order = int(params.get('order', 4))

    if filter_type == 'lowpass':
        cutoff = float(params.get('cutoff', 1000.0))
        return butterworth_lowpass(data_arr, sample_rate, cutoff, order)
    elif filter_type == 'highpass':
        cutoff = float(params.get('cutoff', 20.0))
        return butterworth_highpass(data_arr, sample_rate, cutoff, order)
    elif filter_type == 'bandpass':
        low = float(params.get('low_cutoff', 20.0))
        high = float(params.get('high_cutoff', 2000.0))
        return butterworth_bandpass(data_arr, sample_rate, low, high, order)
    elif filter_type == 'bandstop':
        low = float(params.get('low_cutoff', 45.0))
        high = float(params.get('high_cutoff', 55.0))
        return butterworth_bandstop(data_arr, sample_rate, low, high, order)
    else:
        raise ValueError(f"Unknown filter type: {filter_type}")

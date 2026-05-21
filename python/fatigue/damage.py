"""
Fatigue damage accumulation and life prediction models.

Implements industry-standard fatigue damage calculation methods:
- S-N curve life prediction (Basquin relation)
- Miner's linear cumulative damage rule
- Steinberg three-band method for random vibration fatigue
- Dirlik frequency-domain fatigue damage model
- Per-band damage calculation for spectral fatigue analysis
"""

import math
import numpy as np
from typing import Tuple, List


def sn_curve_life(
    stress_amplitude: float,
    C: float,
    m: float,
) -> float:
    """
    Compute fatigue life from S-N curve using the Basquin relation.

        N_f = C * (sigma_a) ^ (-m)

    where:
        N_f = number of cycles to failure
        C = fatigue strength coefficient (material constant)
        m = fatigue strength exponent (negative of Basquin exponent)
        sigma_a = stress amplitude

    In log-log space: log(N_f) = log(C) - m * log(sigma_a)

    Args:
        stress_amplitude: Stress amplitude (half-range) in MPa.
        C: S-N curve coefficient (material constant).
        m: S-N curve exponent (material constant), typically 3-12.

    Returns:
        Number of cycles to failure N_f.

    Raises:
        ValueError: If stress_amplitude <= 0, C <= 0, or m <= 0.
    """
    if stress_amplitude <= 0:
        raise ValueError(
            f"Stress amplitude must be positive, got {stress_amplitude}"
        )
    if C <= 0:
        raise ValueError(f"S-N coefficient C must be positive, got {C}")
    if m <= 0:
        raise ValueError(f"S-N exponent m must be positive, got {m}")

    return C * (stress_amplitude ** (-m))


def miner_damage(
    cycles: List[float],
    lives: List[float],
) -> float:
    """
    Compute cumulative fatigue damage using Miner's linear damage rule.

        D = sum(n_i / N_i)

    where:
        n_i = number of applied cycles at stress level i
        N_i = cycles to failure at stress level i
        D = cumulative damage (failure when D >= 1.0)

    Miner's rule assumes linear damage accumulation and is the most
    widely used cumulative damage model in aerospace engineering.

    Args:
        cycles: List of applied cycle counts n_i at each stress level.
        lives: List of corresponding fatigue lives N_i at each level.

    Returns:
        Cumulative damage value D (dimensionless).

    Raises:
        ValueError: If cycles and lives have different lengths.

    References:
        M.A. Miner, "Cumulative Damage in Fatigue",
        Journal of Applied Mechanics, 1945.
    """
    if len(cycles) != len(lives):
        raise ValueError(
            f"cycles and lives must have the same length: "
            f"got {len(cycles)} and {len(lives)}"
        )

    damage = 0.0
    for n, N in zip(cycles, lives):
        if N <= 0:
            raise ValueError(f"Fatigue life N must be positive, got {N}")
        damage += n / N

    return damage


def steinberg_damage(
    grms: float,
    fn: float,
    m: float,
    C: float,
    T_seconds: float,
) -> Tuple[float, float]:
    """
    Steinberg three-band method for random vibration fatigue.

    Assumes the stress response is a stationary Gaussian random process
    and partitions the fatigue damage into three stress bands:

        - 1-sigma band: 68.3% of time, stress = 1 * grms
        - 2-sigma band: 27.1% of time, stress = 2 * grms
        - 3-sigma band: 4.33% of time, stress = 3 * grms

    This is the standard approach for electronics and aerospace structures
    under random vibration loading.

    Args:
        grms: Root-mean-square stress in MPa.
        fn: Natural frequency (positive zero-crossing rate) in Hz.
        m: S-N curve exponent.
        C: S-N curve coefficient.
        T_seconds: Exposure duration in seconds.

    Returns:
        Tuple (D, fatigue_life_seconds):
            D: Cumulative damage value.
            fatigue_life_seconds: Predicted fatigue life in seconds
                (inf if D == 0).

    Raises:
        ValueError: If any input parameter is non-positive.

    References:
        Steinberg, D.S., "Vibration Analysis for Electronic Equipment",
        Wiley, 2000.
    """
    if grms <= 0:
        raise ValueError(f"grms must be positive, got {grms}")
    if fn <= 0:
        raise ValueError(f"Natural frequency fn must be positive, got {fn}")
    if m <= 0:
        raise ValueError(f"S-N exponent m must be positive, got {m}")
    if C <= 0:
        raise ValueError(f"S-N coefficient C must be positive, got {C}")
    if T_seconds <= 0:
        raise ValueError(f"Duration must be positive, got {T_seconds}")

    n0 = fn  # positive zero-crossing rate approximates natural frequency

    # Three stress levels
    sigma_1 = 1.0 * grms
    sigma_2 = 2.0 * grms
    sigma_3 = 3.0 * grms

    # Cycle counts in each band
    n1 = 0.683 * n0 * T_seconds
    n2 = 0.271 * n0 * T_seconds
    n3 = 0.0433 * n0 * T_seconds

    # Fatigue life at each stress level
    N1 = sn_curve_life(sigma_1, C, m)
    N2 = sn_curve_life(sigma_2, C, m)
    N3 = sn_curve_life(sigma_3, C, m)

    # Miner's cumulative damage
    D = n1 / N1 + n2 / N2 + n3 / N3

    fatigue_life = T_seconds / D if D > 0 else float('inf')

    return D, fatigue_life


def dirlik_damage(
    psd_freqs: np.ndarray,
    psd_values: np.ndarray,
    m: float,
    C: float,
    T_seconds: float,
) -> Tuple[float, float]:
    """
    Dirlik frequency-domain fatigue damage model.

    Estimates fatigue damage directly from the PSD of the stress response
    without time-domain simulation. Computes spectral moments and uses
    Dirlik's empirical PDF of stress ranges for damage accumulation.

    This is the most widely used frequency-domain fatigue method and is
    validated for both narrow-band and wide-band random processes.

    Algorithm:
        1. Compute spectral moments m0, m1, m2, m4 from PSD
        2. Calculate irregularity factor gamma = m2/sqrt(m0*m4)
        3. Compute Dirlik PDF parameters (D1, D2, D3, R, Q)
        4. Discretize stress range and accumulate Miner damage

    Args:
        psd_freqs: Frequency bins in Hz (1D array).
        psd_values: PSD values in MPa^2/Hz (1D array, same length).
        m: S-N curve exponent.
        C: S-N curve coefficient.
        T_seconds: Exposure duration in seconds.

    Returns:
        Tuple (D, fatigue_life_seconds):
            D: Cumulative damage value.
            fatigue_life_seconds: Predicted fatigue life in seconds
                (inf if D == 0).

    Raises:
        ValueError: If inputs are invalid.

    References:
        Dirlik, T., "Application of Computers in Fatigue Analysis",
        PhD Thesis, University of Warwick, 1985.
    """
    psd_freqs = np.asarray(psd_freqs, dtype=np.float64)
    psd_values = np.asarray(psd_values, dtype=np.float64)

    if len(psd_freqs) != len(psd_values):
        raise ValueError(
            f"psd_freqs and psd_values must have the same length: "
            f"got {len(psd_freqs)} and {len(psd_values)}"
        )
    if len(psd_freqs) < 2:
        raise ValueError("PSD must have at least 2 frequency points")
    if m <= 0:
        raise ValueError(f"S-N exponent m must be positive, got {m}")
    if C <= 0:
        raise ValueError(f"S-N coefficient C must be positive, got {C}")
    if T_seconds <= 0:
        raise ValueError(f"Duration must be positive, got {T_seconds}")

    # Compute spectral moments via trapezoidal integration
    m0 = np.trapz(psd_values, psd_freqs)
    m1 = np.trapz(psd_values * psd_freqs, psd_freqs)
    m2 = np.trapz(psd_values * psd_freqs ** 2, psd_freqs)
    m4 = np.trapz(psd_values * psd_freqs ** 4, psd_freqs)

    if m0 <= 0 or m2 <= 0:
        return 0.0, float('inf')

    # Irregularity factor (bandwidth parameter)
    gamma = m2 / np.sqrt(m0 * m4) if m4 > 0 else 0.5

    # Zero up-crossing rate and peak rate
    e_zero = np.sqrt(m2 / m0)
    e_p = np.sqrt(m4 / m2) if m2 > 0 else e_zero

    # Dirlik parameters
    xm = (m1 / m0) * np.sqrt(m2 / m4) if m4 > 0 else 0.5
    denom = 1.0 + gamma ** 2
    D1 = 2.0 * (xm - gamma ** 2) / denom
    D2 = (1.0 - gamma - D1 + D1 ** 2) / max(1.0 - gamma, 1e-10)
    D3 = 1.0 - D1 - D2
    R = (gamma - xm - D1 ** 2) / max(1.0 - gamma - D1 + D1 ** 2, 1e-10)
    Q = 1.25 * (gamma - D3 - D2 * R) / max(D1, 1e-10)

    # Discretize stress range
    S_min = 0.01 * np.sqrt(m0)
    S_max = 5.0 * np.sqrt(m0)
    S = np.linspace(S_min, S_max, 200)
    dS = S[1] - S[0]

    # Dirlik probability density function
    Z = S / (2.0 * np.sqrt(m0))
    pdf = (
        (D1 / max(Q, 1e-10)) * np.exp(-Z / max(Q, 1e-10))
        + (D2 * Z / max(R ** 2, 1e-10)) * np.exp(-Z ** 2 / (2.0 * max(R ** 2, 1e-10)))
        + D3 * Z * np.exp(-Z ** 2 / 2.0)
    )
    pdf /= (2.0 * np.sqrt(m0))
    pdf = np.maximum(pdf, 0.0)

    # Total number of cycles
    total_cycles = e_p * T_seconds

    # Damage accumulation via Miner's rule
    D = 0.0
    for i in range(len(S) - 1):
        s_mid = (S[i] + S[i + 1]) / 2.0
        n_i = pdf[i] * dS * total_cycles
        if n_i <= 0:
            continue
        N_i = sn_curve_life(s_mid, C, m)
        D += n_i / N_i

    fatigue_life = T_seconds / D if D > 0 else float('inf')
    return D, fatigue_life


def band_damage(
    amplitude_distribution: List[float],
    counts: List[float],
    m: float,
    C: float,
) -> float:
    """
    Calculate fatigue damage for a single frequency band.

    For each amplitude level in the band, compute the corresponding
    fatigue life from the S-N curve and accumulate using Miner's rule.

    This is the core computation used in the equal-damage spectrum
    compilation pipeline.

    Args:
        amplitude_distribution: Stress amplitudes for each bin
            in the frequency band (e.g., rainflow amplitudes).
        counts: Number of cycles at each amplitude level.
        m: S-N curve exponent.
        C: S-N curve coefficient.

    Returns:
        Cumulative damage D for this frequency band.

    Raises:
        ValueError: If arrays have different lengths or invalid parameters.
    """
    if len(amplitude_distribution) != len(counts):
        raise ValueError(
            f"amplitude_distribution and counts must have same length: "
            f"got {len(amplitude_distribution)} and {len(counts)}"
        )
    if m <= 0:
        raise ValueError(f"S-N exponent m must be positive, got {m}")
    if C <= 0:
        raise ValueError(f"S-N coefficient C must be positive, got {C}")

    damage = 0.0
    for amp, count in zip(amplitude_distribution, counts):
        if amp <= 0 or count <= 0:
            continue
        N = sn_curve_life(amp, C, m)
        damage += count / N

    return damage

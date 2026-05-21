"""
Mean stress correction methods for fatigue analysis.

Implements Goodman, Morrow, and Soderberg corrections to convert
actual stress amplitudes to equivalent zero-mean stress amplitudes.
These corrections account for the effect of mean stress on fatigue life,
which is critical for accurate damage accumulation in acoustic fatigue
load spectrum compilation.
"""

import numpy as np
from typing import List, Union


def goodman_correction(
    stress_amplitude: Union[float, np.ndarray],
    mean_stress: Union[float, np.ndarray],
    uts: float,
) -> Union[float, np.ndarray]:
    """
    Goodman mean stress correction.

    Converts non-zero-mean stress amplitudes to equivalent fully-reversed
    (zero-mean) stress amplitudes using the Goodman relation:

        sigma_ar = sigma_a / (1 - sigma_m / sigma_uts)

    This is the most commonly used correction for ductile materials in
    aerospace applications.

    Args:
        stress_amplitude: Stress amplitude (half-range) in MPa.
        mean_stress: Mean stress in MPa.
        uts: Ultimate tensile strength in MPa.

    Returns:
        Equivalent zero-mean stress amplitude in MPa.

    Raises:
        ValueError: If uts <= 0 or if mean_stress >= uts
            (which would imply static failure).

    References:
        J. Goodman, "Mechanics Applied to Engineering", 1899.
    """
    if uts <= 0:
        raise ValueError(f"UTS must be positive, got {uts}")

    stress_amplitude = np.asarray(stress_amplitude, dtype=np.float64)
    mean_stress = np.asarray(mean_stress, dtype=np.float64)

    ratio = mean_stress / uts
    if np.any(ratio >= 1.0):
        raise ValueError(
            "Mean stress exceeds UTS — static failure would occur. "
            f"max(mean_stress/uts) = {np.max(ratio):.4f}"
        )

    corrected = stress_amplitude / (1.0 - ratio)
    return float(corrected) if corrected.ndim == 0 else corrected


def goodman_correct_rainflow(
    amplitudes: List[float],
    means: List[float],
    uts: float,
) -> List[float]:
    """
    Apply Goodman correction to rainflow counting results in batch.

    Processes all (amplitude, mean) pairs from rainflow counting output
    and returns corrected equivalent zero-mean amplitudes for subsequent
    damage calculation.

    Args:
        amplitudes: List of stress amplitudes from rainflow counting.
        means: List of corresponding mean stress values.
        uts: Ultimate tensile strength in MPa.

    Returns:
        List of Goodman-corrected equivalent stress amplitudes.

    Raises:
        ValueError: If amplitudes and means have different lengths,
            or if uts <= 0.
    """
    if len(amplitudes) != len(means):
        raise ValueError(
            f"amplitudes and means must have the same length: "
            f"got {len(amplitudes)} and {len(means)}"
        )
    if uts <= 0:
        raise ValueError(f"UTS must be positive, got {uts}")

    amps_arr = np.asarray(amplitudes, dtype=np.float64)
    means_arr = np.asarray(means, dtype=np.float64)

    corrected = goodman_correction(amps_arr, means_arr, uts)
    return corrected.tolist() if hasattr(corrected, 'tolist') else [float(corrected)]


def morrow_correction(
    stress_amplitude: Union[float, np.ndarray],
    mean_stress: Union[float, np.ndarray],
    fatigue_strength_coefficient: float,
) -> Union[float, np.ndarray]:
    """
    Morrow mean stress correction.

    Uses the fatigue strength coefficient (sigma_f') rather than UTS
    as the limiting stress. This is generally more accurate than Goodman
    for materials where the fatigue strength coefficient differs
    significantly from UTS.

        sigma_ar = sigma_a / (1 - sigma_m / sigma_f')

    Args:
        stress_amplitude: Stress amplitude (half-range) in MPa.
        mean_stress: Mean stress in MPa.
        fatigue_strength_coefficient: Fatigue strength coefficient
            (sigma_f') in MPa, typically from strain-life data.

    Returns:
        Equivalent zero-mean stress amplitude in MPa.

    Raises:
        ValueError: If fatigue_strength_coefficient <= 0 or if
            mean_stress >= fatigue_strength_coefficient.

    References:
        Morrow, J.D., "Fatigue Design Handbook", SAE, 1968.
    """
    if fatigue_strength_coefficient <= 0:
        raise ValueError(
            f"Fatigue strength coefficient must be positive, "
            f"got {fatigue_strength_coefficient}"
        )

    stress_amplitude = np.asarray(stress_amplitude, dtype=np.float64)
    mean_stress = np.asarray(mean_stress, dtype=np.float64)

    ratio = mean_stress / fatigue_strength_coefficient
    if np.any(ratio >= 1.0):
        raise ValueError(
            "Mean stress exceeds fatigue strength coefficient. "
            f"max(mean_stress/sigma_f') = {np.max(ratio):.4f}"
        )

    corrected = stress_amplitude / (1.0 - ratio)
    return float(corrected) if corrected.ndim == 0 else corrected


def soderberg_correction(
    stress_amplitude: Union[float, np.ndarray],
    mean_stress: Union[float, np.ndarray],
    yield_strength: float,
) -> Union[float, np.ndarray]:
    """
    Soderberg mean stress correction (conservative).

    Uses the yield strength as the limiting stress, making this the most
    conservative of the three common corrections. Suitable for
    safety-critical applications or materials with limited ductility.

        sigma_ar = sigma_a / (1 - sigma_m / sigma_y)

    Args:
        stress_amplitude: Stress amplitude (half-range) in MPa.
        mean_stress: Mean stress in MPa.
        yield_strength: Yield strength in MPa.

    Returns:
        Equivalent zero-mean stress amplitude in MPa.

    Raises:
        ValueError: If yield_strength <= 0 or if
            mean_stress >= yield_strength.

    References:
        Soderberg, C.R., "Factor of Safety and Working Stress",
        Trans. ASME, 1930.
    """
    if yield_strength <= 0:
        raise ValueError(
            f"Yield strength must be positive, got {yield_strength}"
        )

    stress_amplitude = np.asarray(stress_amplitude, dtype=np.float64)
    mean_stress = np.asarray(mean_stress, dtype=np.float64)

    ratio = mean_stress / yield_strength
    if np.any(ratio >= 1.0):
        raise ValueError(
            "Mean stress exceeds yield strength — yielding would occur. "
            f"max(mean_stress/sigma_y) = {np.max(ratio):.4f}"
        )

    corrected = stress_amplitude / (1.0 - ratio)
    return float(corrected) if corrected.ndim == 0 else corrected

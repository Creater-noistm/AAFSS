"""
Fatigue analysis module for acoustic fatigue load spectrum compilation.

Provides:
- Mean stress correction methods (Goodman, Morrow, Soderberg)
- S-N curve life prediction
- Miner's linear cumulative damage rule
- Steinberg three-band method for random vibration fatigue
- Dirlik frequency-domain fatigue damage model
"""

from .goodman import (
    goodman_correction,
    goodman_correct_rainflow,
    morrow_correction,
    soderberg_correction,
)

from .damage import (
    sn_curve_life,
    miner_damage,
    steinberg_damage,
    dirlik_damage,
    band_damage,
)

__all__ = [
    "goodman_correction",
    "goodman_correct_rainflow",
    "morrow_correction",
    "soderberg_correction",
    "sn_curve_life",
    "miner_damage",
    "steinberg_damage",
    "dirlik_damage",
    "band_damage",
]

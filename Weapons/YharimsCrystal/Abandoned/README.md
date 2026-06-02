# Abandoned YharimsCrystal Implementations

This directory contains legacy YharimsCrystal gameplay code retained only as reference.

`LegacyFourModes` contains the removed drill, flamethrower, warship, and helix-laser modes together with their former mode-switch state. The active weapon no longer creates or selects these holdouts. Their namespaces are intentionally unchanged so the archived code remains readable and compilable while shared dependencies are separated from the active implementation.

The current implementation lives in `Weapons/YharimsCrystal/MainAttack/E_TyrantPrism`.

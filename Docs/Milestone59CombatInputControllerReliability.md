# M59: Combat Input + Controller Reliability Pass

M59 locks gameplay input around the Unity Input System.

- Keyboard target: WASD move, arrows aim, J light, K heavy, E interact, Tab swap, Q active, F card, Shift guard, Escape pause.
- DualShock 5 target: left stick move, right stick aim, Cross interact, L1 swap, R1 light, R2 heavy, Triangle active, Square card, L2 guard, Options pause.
- Gameplay routes should not use legacy `UnityEngine.Input`.
- Debug UI should prefer visible buttons/toggles where function keys are unreliable.


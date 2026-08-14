# R8 Physical Validation --- Overview

## Goal

Move ThermoCore from simulation-only confidence to measurement-backed
validation.

Workflow: **simulation → measurement → import → calibration → holdout
validation → model revision**.

## Hardware already available

-   **Mirabelle Electric Cooler E24**, type `7706 11000000`, 12 V, 48 W,
    climate class N. Treat first as a complete thermoelectric black box.
    Do not assume the internal TEC part number.
-   **Siguro SGR-DH-P300W / DH-P300W Orca 20 l WiFi**. Manufacturer
    rating: 330 W input, up to 20 L/day, 6.5 L tank. Use as the real
    vapor-compression baseline.

## R8 stages

1.  Instrumentation and data contract.
2.  Mirabelle TEC-system characterization.
3.  Siguro compressor-system characterization.
4.  Controlled dew-point tests.
5.  Sorbent A/B/C experiments.
6.  Calibration + holdout validation.
7.  Optional instrumented bare-TEC rig.
8.  Outdoor 24--72 h campaign.

## Principle

Do not buy/build a custom compressor during R8. The existing Siguro
already provides a complete compressor/evaporator/condenser/fan
reference.

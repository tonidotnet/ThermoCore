# R3-002 — Commercial Peltier Dehumidifier Black-Box Model

Requirements:
COOL-005.

Purpose:
allow measured commercial-unit behavior to serve as an empirical baseline.

Inputs should include:
- inlet moist-air state;
- airflow if known;
- electrical power/control state.

Outputs:
- outlet moist-air state where data supports it;
- water rate;
- electrical power;
- diagnostics;
- model validity range.

Calibration must use existing calibration infrastructure.

Acceptance:
- no undocumented extrapolation;
- provenance/evidence metadata recorded;
- can be compared with analytical TEC path using common KPIs.

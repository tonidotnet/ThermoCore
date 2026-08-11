# R3-001 — Prototype Measurement and Hardware Metadata

Requirements:
VAL-001, VAL-002, VAL-003.

Extend the existing calibration/measurement pipeline only where necessary.

Support:
- commercial Peltier dehumidifier baseline;
- hardware identity;
- sensor calibration IDs;
- inlet/outlet T/RH;
- voltage/current/power;
- water mass;
- optional airflow and surface temperatures.

Do not create a parallel calibration subsystem.

Acceptance:
- CSV import works for the proposed prototype schema;
- hardware metadata is preserved;
- validation level can distinguish Bench/Integrated/Outdoor.

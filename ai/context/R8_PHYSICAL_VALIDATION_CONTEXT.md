# R8 Physical Validation Context

R8 is measurement-first. Existing hardware: Mirabelle E24 thermoelectric
cooler (12 V, 48 W) and Siguro SGR-DH-P300W compressor dehumidifier
(manufacturer rating 330 W, up to 20 L/day).

Do not assume the Mirabelle internal TEC model until inspected. Reuse
ThermoCore's existing calibration/holdout infrastructure. Preserve raw
measurements. Catalog ratings are metadata, not measured data.

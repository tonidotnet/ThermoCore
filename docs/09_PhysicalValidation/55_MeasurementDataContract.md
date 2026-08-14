# R8 Measurement Data Contract

## Metadata

`testId, variantId, hardwareProfileId, startUtc, endUtc, validationLevel, notes`

## Time series

`timestampUtc, ambientTemperatureC, ambientRhPercent, inletTemperatureC, inletRhPercent, outletTemperatureC, outletRhPercent, coldSurfaceTemperatureC, hotSurfaceTemperatureC, airflowM3PerHour, voltageV, currentA, powerW, energyWh, waterMassG, sorbentMassG, solarIrradianceWPerM2, compressorState, fanState`

## Derived by ThermoCore

`inletDewPointC, outletDewPointC, inletHumidityRatio, outletHumidityRatio, waterRateGPerHour, electricWhPerLiter, coolingPlantCop, dewPointMarginK`

Derived values should not replace raw measurements.

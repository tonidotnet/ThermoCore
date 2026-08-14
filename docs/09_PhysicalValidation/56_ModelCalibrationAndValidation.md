# Model Calibration and Validation

Sequence: 1. import raw campaign; 2. validate units/timestamps; 3. apply
sensor calibration; 4. flag invalid samples; 5. split calibration and
holdout; 6. fit identifiable parameters only; 7. run holdout; 8. produce
model-vs-measurement report.

For TEC, identify effective hot/cold thermal resistance and air-side UA
before overfitting intrinsic TEC coefficients.

For Siguro black-box calibration, identify water-rate/power/cycling
behavior versus inlet state. Do not infer refrigerant internals from
external measurements.

Report MAE, RMSE, bias, total-water error and total-energy error.

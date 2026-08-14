# R8 Instrumentation and Sensors

## Minimum measurements

-   ambient T/RH;
-   DUT inlet T/RH;
-   DUT outlet T/RH;
-   cold surface / evaporator temperature where accessible;
-   hot-side / condenser temperature where accessible;
-   electrical power and accumulated Wh;
-   collected condensate mass, preferably 1 g resolution.

## Optional

-   airflow;
-   solar irradiance;
-   sorbent mass/loading;
-   multiple heat-exchanger temperatures.

## Suggested automated sensor classes

-   SHT4x-class T/RH;
-   DS18B20 or thermocouple for surfaces;
-   INA226-class DC voltage/current/power;
-   certified plug-in 230 V Wh meter for Siguro;
-   load cell + HX711 for continuous water mass.

## Calibration metadata

Every sensor needs ID, model, calibration date, correction, uncertainty
and evidence source.

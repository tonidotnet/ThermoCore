# AWG Simulator

## 01_ProjectOverview.md



Version: 1.0



Author: Project Specification



Status: Draft



---



# 1. Project Name



Atmospheric Water Generator Simulator (AWG Simulator)



---



# 2. Project Goal



The goal of this project is to develop a physics-based simulator for a portable

Atmospheric Water Generator (AWG) that extracts drinking water from ambient air

using:



- Solar thermal energy

- Photovoltaic energy

- Silica gel adsorption/desorption

- Peltier-assisted condensation

- Heat recovery



The simulator shall accurately model the complete thermodynamic process and allow

rapid evaluation of different mechanical designs before building physical

prototypes.



The software is intended to become the digital twin of the future hardware.



---



# 3. Project Scope



The simulator shall model the following physical systems.



## Included



✔ Ambient air



✔ Solar collector



✔ Solar panel



✔ Air channels



✔ Fans



✔ Peltier modules



✔ Silica gel



✔ Condensation chamber



✔ Water tank



✔ Battery



✔ MPPT charging



✔ Heat recovery



✔ Weather conditions



✔ Daily simulation



✔ Energy balance



✔ Mass balance



✔ Water production



---



## Not included in Version 1



✖ CFD simulation



✖ Finite element analysis



✖ Structural mechanics



✖ Detailed electronic circuit simulation



✖ Moisture diffusion inside silica particles



✖ Mechanical CAD



---



# 4. Main Objectives



The simulator shall answer engineering questions such as



- How much water can be produced per day?



- What collector size is optimal?



- How much silica gel is required?



- What Peltier power is optimal?



- What airflow maximizes efficiency?



- What battery capacity is required?



- What happens under different weather conditions?



---



# 5. High Level System Architecture



```

Ambient Air



↓



Solar Collector



↓



Peltier Hot Side



↓



Silica Gel



↓



Condensation Chamber



↓



Heat Recovery



↓



Exhaust

```



Electrical system



```

Sun



↓



Solar Panel



↓



MPPT



↓



Battery



↓



Fans



↓



Peltier



↓



Electronics

```



---



# 6. Hardware Concept



Portable outdoor unit.



Maximum footprint:



1000 mm × 1000 mm



Maximum height:



350 mm



Maximum weight target:



25 kg



Designed for outdoor operation.



---



# 7. Operating Principle



The complete cycle consists of two major phases.



---



## Phase A



Water adsorption



Ambient air passes through silica gel.



The silica gel adsorbs water from the air.



Dry air exits the system.



---



## Phase B



Water regeneration



Solar collector heats air.



The hot air regenerates the silica gel.



Released moisture enters the condensation chamber.



Peltier modules cool the condenser.



Liquid water is collected.



---



# 8. Design Philosophy



The simulator shall prioritize



Accuracy



Repeatability



Modularity



Maintainability



Extensibility



The software is expected to evolve together with the physical prototype.



---



# 9. Digital Twin Concept



The simulator is intended to become a digital twin.



Future measured data shall be used to calibrate



collector efficiency



silica gel model



Peltier model



fan performance



heat losses



water production



---



# 10. Simulation Resolution



Initial implementation



Time step



1 minute



Simulation length



24 hours



Future versions



10 seconds



1 second



Real-time simulation



---



# 11. Physical Domains



The simulator combines



Thermodynamics



Fluid mechanics



Psychrometrics



Heat transfer



Mass transfer



Electrical energy balance



Solar radiation



Adsorption



Condensation



---



# 12. Simulation Inputs



Outdoor temperature



Outdoor humidity



Atmospheric pressure



Solar radiation



Wind speed



Solar panel size



Collector size



Collector efficiency



Airflow



Fan power



Peltier power



Battery size



Silica gel mass



Heat exchanger efficiency



Simulation duration



Time step



---



# 13. Simulation Outputs



Water production



Condensed water



Collector temperature



Collector efficiency



Air temperature



Relative humidity



Absolute humidity



Dew point



Silica gel saturation



Battery SOC



Solar production



Peltier consumption



Fan consumption



Energy balance



Mass balance



---



# 14. Software Architecture



Solution



AWGSimulator.sln



Projects



AWG.Core



AWG.Console



AWG.UI



The Core project shall contain all physical calculations.



No UI code shall exist inside AWG.Core.



---



# 15. Data Flow



Configuration



↓



Simulation Engine



↓



Physical Components



↓



Air State



↓



Simulation Results



↓



CSV



↓



Charts



---



# 16. Development Phases



Version 0.1



Psychrometric calculations



Version 0.2



Solar collector



Version 0.3



Silica gel



Version 0.4



Peltier



Version 0.5



Condensation



Version 0.6



Battery



Version 0.7



Heat recovery



Version 0.8



Full energy balance



Version 0.9



Calibration



Version 1.0



Prototype validation



---



# 17. Future Extensions



Weather API



Live sensor data



Automatic calibration



Optimization algorithms



Machine learning



Multiple collector geometries



Multiple Peltier modules



Alternative desiccants



Night cooling



Hybrid operation



---



# 18. Success Criteria



The simulator shall predict



daily water production



within ±10%



when compared to the future physical prototype.



---



# 19. Assumptions



Ideal gas approximation



Uniform airflow



Uniform collector temperature



Uniform silica gel temperature



Constant atmospheric pressure



No air leakage



No mechanical deformation



No rain effects



---



# 20. Risks



Unknown silica gel kinetics



Real Peltier COP variation



Collector heat losses



Condensation efficiency



Fan curve deviations



Weather uncertainty



Prototype manufacturing tolerances



---



# 21. Documentation Structure



01_ProjectOverview



02_SystemRequirements



03_PhysicsModel



04_Psychrometrics



05_SolarCollector



06_SolarPanel



07_Peltier



08_SilicaGel



09_Condenser



10_HeatRecovery



11_Battery



12_SimulationEngine



13_ClassModel



14_API



15_Algorithms



16_TestCases



17_Roadmap



18_CodingRules



---



End of Document


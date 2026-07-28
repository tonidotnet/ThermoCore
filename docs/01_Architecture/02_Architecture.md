# AWG Simulator

## 02_SystemRequirements.md



Version: 1.0



Document Type: Software Requirements Specification (SRS)



Status: Draft



---



# 1. Purpose



This document specifies the functional and non-functional requirements of the

Atmospheric Water Generator Simulator.



The requirements defined here are intended to be directly implementable by both

human developers and AI-assisted code generation systems.



---



# 2. Scope



The simulator shall calculate the thermodynamic behavior of a portable

Atmospheric Water Generator (AWG) using time-based numerical simulation.



The simulation shall include:



- Air psychrometrics

- Solar thermal energy

- Photovoltaic generation

- Peltier cooling/heating

- Silica gel adsorption/desorption

- Condensation

- Heat recovery

- Battery charging/discharging



---



# 3. Target Platform



Operating System



Windows 11



Development Platform



.NET 10



Language



C# 15



IDE



Visual Studio 2022 or newer



---



# 4. Solution Structure



The solution shall contain three projects.



AWG.Core



Contains all simulation logic.



No UI dependencies allowed.



AWG.Console



Console application used for debugging and regression testing.



AWG.UI



WPF application used for visualization.



---



# 5. Functional Requirements



## FR-001 Configuration



The application shall load its configuration from JSON files.



Priority



Critical



Acceptance Criteria



- Invalid JSON shall generate descriptive validation errors.

- Missing mandatory properties shall prevent simulation start.



---



## FR-002 Simulation



The simulator shall execute a complete time-based simulation.



Acceptance Criteria



- User specifies duration.

- User specifies timestep.

- Engine returns all simulation states.



---



## FR-003 Weather Conditions



The simulator shall support configurable environmental conditions.



Minimum supported parameters



Outdoor Temperature



Outdoor Relative Humidity



Atmospheric Pressure



Wind Speed



Solar Radiation



---



## FR-004 Solar Collector



The simulator shall calculate



Collector outlet temperature



Collector efficiency



Heat losses



Delivered thermal energy



---



## FR-005 Solar Panel



The simulator shall calculate



Electrical output



Panel temperature



Panel efficiency



Available electrical energy



---



## FR-006 Fan



The simulator shall calculate



Airflow



Electrical consumption



Pressure losses



---



## FR-007 Peltier



The simulator shall calculate



Cooling capacity



Heating capacity



COP



Electrical consumption



Temperature difference



---



## FR-008 Silica Gel



The simulator shall calculate



Water adsorption



Water desorption



Current saturation



Maximum capacity



Regeneration state



---



## FR-009 Condensation



The simulator shall calculate



Condensed water



Condensation efficiency



Heat released



Remaining moisture



---



## FR-010 Heat Recovery



The simulator shall calculate



Recovered heat



Heat exchanger effectiveness



Pressure losses



---



## FR-011 Battery



The simulator shall calculate



SOC



Charging power



Discharging power



Remaining capacity



---



## FR-012 Water Production



The simulator shall calculate



Instantaneous water production



Hourly water production



Daily water production



Cumulative production



---



## FR-013 CSV Export



The simulator shall export all simulation results.



Format



CSV



Encoding



UTF-8



Delimiter



Semicolon



---



## FR-014 Console Report



The console application shall generate a readable summary.



Minimum information



Produced Water



Collector Peak Temperature



Maximum Peltier Power



Battery SOC



Simulation Duration



---



## FR-015 Graphical Interface



The WPF application shall provide



Configuration editor



Simulation start



Simulation progress



Charts



Simulation summary



---



# 6. Non-Functional Requirements



---



## NFR-001 Performance



The simulator shall complete



24-hour simulation



within



5 seconds



on a typical desktop PC.



---



## NFR-002 Memory



Maximum RAM usage



500 MB



---



## NFR-003 Precision



Floating point



double



shall be used throughout the project.



float shall never be used.



---



## NFR-004 Deterministic Execution



Identical inputs



must always produce



identical outputs.



---



## NFR-005 Thread Safety



Simulation calculations



shall be independent from UI thread.



---



## NFR-006 Modularity



Every physical subsystem



shall be implemented independently.



---



## NFR-007 Extensibility



Adding a new physical subsystem



shall not require modification of existing modules.



---



## NFR-008 Documentation



Every public class



shall contain XML documentation.



---



## NFR-009 Logging



Simulation shall generate



structured log messages.



---



## NFR-010 Unit Tests



Every calculation module



shall be independently testable.



---



# 7. Configuration Requirements



The simulator shall support JSON configuration.



Example



Simulation duration



Time step



Collector size



Panel size



Fan settings



Battery



Peltier



Silica gel



Heat exchanger



Weather



---



# 8. Validation Rules



Outdoor temperature



-40°C ... +60°C



Outdoor humidity



0...100 %



Solar radiation



0...1400 W/m²



Collector efficiency



0...100 %



Battery SOC



0...100 %



---



# 9. Error Handling



Invalid configuration



shall generate



ConfigurationException



Invalid physical values



shall generate



ValidationException



Calculation failures



shall generate



SimulationException



---



# 10. Output Data



Simulation shall store



Time



Air temperature



Relative humidity



Absolute humidity



Dew point



Enthalpy



Collector temperature



Collector efficiency



Solar power



Battery SOC



Peltier power



Fan power



Condensed water



Silica gel saturation



---



# 11. Acceptance Tests



AT-001



Load configuration



PASS if valid configuration loads.



---



AT-002



Run simulation



PASS if simulation completes.



---



AT-003



CSV Export



PASS if exported CSV contains all required columns.



---



AT-004



Water Production



PASS if condensed water is never negative.



---



AT-005



Battery



PASS if SOC remains between



0%



and



100%.



---



AT-006



Humidity



PASS if RH always remains



between



0%



and



100%.



---



AT-007



Energy Balance



PASS if total energy error



remains below



1%.



---



AT-008



Mass Balance



PASS if total water mass error



remains below



0.5%.



---



# 12. Coding Constraints



No UI code inside Core.



No static mutable state.



No magic numbers.



All constants shall have documented sources.



SI units shall be used internally.



---



# 13. Future Requirements



Weather API



Real sensor input



Automatic calibration



Optimization engine



Machine learning



Cloud synchronization



---



End of Document


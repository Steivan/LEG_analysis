

# Stochastic Generative Model for PV Production and Consumption

## Introduction
This document outlines a stochastic generative model for simulating photovoltaic (PV) production and household consumption at two sites in Maur, Switzerland (8124). The sites, operational since October 2018 and September 2021, include PV installations, batteries, household loads, heat-pumps, and a one-directional wall-box (second site). The model combines idealized PV production (geometry-based) with weather effects and consumption profiles, using Monte Carlo (MC) simulations to capture uncertainty at monthly, daily, hourly, and 15-minute resolutions. Parameters are calibrated using a Bayesian approach with historic data (PV production/consumption, MeteoSwiss weather data). The model supports both retrospective (2000–2025) and prospective scenarios, leveraging weather forecasts or MC simulations when forecasts are unavailable.

## PV Production Model

### Idealized Production
The idealized PV production is pre-computed based on roof geometry (latitude, longitude, orientation, slope) and installed capacity ($P_{\text{inst}}$ in kWp). The model uses the cosine of the angle between the sun and roof normal, computed for any day and time, to estimate baseline power. For a time step $t$ (15-minute interval), the idealized production $P_{\text{ideal}}(t)$ (in kWh) is:

$$P_{\text{ideal}}(t) = P_{\text{inst}} \cdot G_{\text{ideal}}(t) \cdot \Delta t$$

Where:
- $P_{\text{inst}}$: Installed PV capacity (kWp).
- $G_{\text{ideal}}(t)$: Idealized solar irradiance (kW/m²), based on roof geometry and solar position.
- $\Delta t$: Time step (0.25 hours for 15 minutes).

### Weather Effects
Weather impacts production through temperature, irradiance, and other factors (clouds, fog, snow, wind). The actual production $P_{\text{actual}}(t)$ is modeled as:

$$P_{\text{actual}}(t) = P_{\text{ideal}}(t) \cdot \eta(t) \cdot f_{\text{GHI}}(t) \cdot f_{\text{temp}}(t) \cdot f_{\text{wind}}(t) \cdot f_{\text{local}}(t)$$

Where:
- $\eta(t)$: System efficiency (e.g., 0.15–0.20, accounting for inverter losses).
- $f_{\text{GHI}}(t)$: Global horizontal irradiance (GHI) factor, ratio of actual GHI ($G_{\text{act}}(t)$) to idealized GHI:

$$f_{\text{GHI}}(t) = \frac{G_{\text{act}}(t)}{G_{\text{ideal}}(t)}$$

$G_{\text{act}}(t)$ is sampled from MeteoSwiss `gre000s0` data or MC simulations.
- $f_{\text{temp}}(t)$: Temperature correction, based on air temperature $T(t)$ (from `tre200s0`):

$$f_{\text{temp}}(t) = 1 - \beta (T(t) - T_{\text{ref}})$$

Where $\beta$ is the temperature coefficient (e.g., 0.004/°C), $T_{\text{ref}} = 25^\circ \text{C}$.
- $f_{\text{wind}}(t)$: Wind cooling factor, improving efficiency (e.g., linear increase with wind speed $W(t)$).
- $f_{\text{local}}(t)$: Local adjustment factor for site-specific effects (e.g., morning fog in Maur due to proximity to Greifensee and ridges). Derived by comparing observed site production to regional estimates (idealized production adjusted by MeteoSwiss weather). Fog probability may depend on predictors like dew point, temperature, and low wind speed, especially in fall/winter mornings.

### Terrain-Adjusted Solar Production for Alpine Sites

#### Problem Statement
Idealized PV production (`P_ideal(t)`) assumes a flat horizon, suitable for lowland sites like Maur (8124), where sunrise/sunset align with geometric sun position (sun_elev > 0°). In alpine valleys like Scuol (GR, 46.833°N, 10.283°E, ~1300m elev), surrounding mountains delay sunrise and advance sunset (e.g., +30–60 min), reducing insolation by 10–25% annually. This requires a site-specific adjustment to `f_local(t)` in:

$$P_{\text{actual}}(t) = P_{\text{ideal}}(t) \cdot \eta(t) \cdot f_{\text{GHI}}(t) \cdot f_{\text{temp}}(t) \cdot f_{\text{wind}}(t) \cdot f_{\text{local}}(t)$$

#### Proposed Solutions
1. **Dynamic Horizon Profile (Recommended)**  
   Model horizon elevation as a function of azimuth using Digital Elevation Model (DEM) data. For each 15-min timestep:  
   - Compute sun position (azimuth/elevation via NodaTime).  
   - Set `f_local(t) = 0` if sun_elev < horizon_elev(azimuth), else 1 (or fractional for partial shading).  
   - **Automation**: Query Google Elevation API for radial elevation profiles (36 azimuths, 50 km rays, ~30m res). Cache as lookup table.  
   - **Pros**: Precise, automates terrain effects. **Cons**: API rate limits (mitigated by caching). **Error**: <5% vs. empirical.

2. **Interpolated Sunrise/Sunset Times (Fallback)**  
   Sample terrain-adjusted sunrise/sunset times (e.g., 4 days/month: 1st, 8th, 15th, 23rd) via APIs (e.g., SunriseSunset.io + DEM adjustments). Fit Fourier series:  
   $$t_{\text{sunrise}}(day) = a_0 + \sum_{k=1}^4 \left( a_k \cos\left(\frac{2\pi k day}{365}\right) + b_k \sin\left(\frac{2\pi k day}{365}\right) \right)$$  
   Set `f_local(t) = 0` outside sunrise–sunset.  
   - **Historical Method**: Manual Google Earth extracts. **Automation**: Google Elevation API for horizon-derived times.  
   - **Pros**: Simpler, low compute. **Cons**: Ignores intra-day shading. **Error**: 5–10%.

3. **Empirical Corrections**  
   Scale `P_actual(t)` using observed production ratios from nearby sites (e.g., MeteoSwiss stations). Adjust `f_local(t)` empirically.  
   - **Pros**: No new data. **Cons**: Coarse, assumes similar microclimate.

#### Implementation
- **Data**: Google Elevation API (`https://maps.googleapis.com/maps/api/elevation/json`) for horizon profiles; fallback to Meteomatics Sun API for adjusted times.  
- **Integration**: Extend `StationMetaImporter` with `HorizonProfile` class. Cache profiles (Maur: flat; Scuol: DEM-derived). Update `f_local(t)` in `EnergyModel.cs`.  
- **Validation**: Compare Scuol outputs vs. manual Google Earth times (e.g., Jan 15 sunrise ~08:20 vs. flat ~07:45).  
- **Next Steps**: Prototype Approach 1 for Scuol (46.833°N, 10.283°E); test API limits.

### Stochastic Modeling
Weather parameters are modeled hierarchically via MC simulations, focusing on daytime data (since PV production is zero at night). The approach captures seasonal periodicity, daily autocorrelation, and short-term fluctuations, using a 30-minute resolution to align MeteoSwiss (10-min) and PV data (15-min).

- **Seasonal (Annual):** Expected values for parameters (e.g., GHI, temperature) are modeled with Fourier series (3rd or 4th order) to capture annual periodicity. Coefficients are fitted via least squares on daily-averaged MeteoSwiss data (2000–2025, e.g., `gre000s0`, `tre200s0`). Example for temperature mean:

$$\mu_m(t) = a_0 + \sum_{k=1}^4 \left( a_k \cos\left(\frac{2\pi k t}{365}\right) + b_k \sin\left(\frac{2\pi k t}{365}\right) \right)$$

Where $t$ is day of year, $a_k$, $b_k$ are fitted coefficients. Long-term trends (e.g., global warming) are ignored initially but can be added via detrending if needed.

- **Daily:** Daily values are simulated with autocorrelation to reflect persistence (e.g., sunny spells). Use an AR(1) model for each parameter:

$$X_t = \phi X_{t-1} + \epsilon_t, \quad \epsilon_t \sim \mathcal{N}(0, \sigma_\epsilon^2)$$

Where $X_t$ is the deviation from the Fourier mean (e.g., $T(t) - \mu_m(t)$), $\phi$ is the autocorrelation coefficient (fitted per season via ACF/PACF), and $\epsilon_t$ is Gaussian noise. Seasonal variations in $\phi$ are checked by stratifying data (e.g., winter vs. summer).

- **Diurnal (Hourly):** Average diurnal patterns are modeled with Fourier series, fitted to hourly data per season or month to account for varying daylight. Example for GHI:

$$G_{\text{act,diurnal}}(h) = c_0 + \sum_{k=1}^3 \left( c_k \cos\left(\frac{2\pi k h}{24}\right) + d_k \sin\left(\frac{2\pi k h}{24}\right) \right)$$

Where $h$ is hour of day, $c_k$, $d_k$ are fitted coefficients.

- **Short-Term (30-minute):** Short-term fluctuations (e.g., cloud passages) are modeled with a Markov chain for cloud states (clear, partly cloudy, overcast), with transition probabilities conditioned on daily cloudiness (from MeteoSwiss sunshine duration or GHI). Each state maps to a variance multiplier for $f_{\text{GHI}}(t)$. Within states, use AR(1) for autocorrelation:

$$G_{\text{act}}(t) = \phi_{\text{short}} G_{\text{act}}(t-1) + \epsilon_t^{\text{short}}, \quad \epsilon_t^{\text{short}} \sim \mathcal{N}(0, \sigma_{\text{short}}^2)$$

For each time step $t$ (30-min):
1. Compute seasonal mean via Fourier series (e.g., $\mu_m(t)$ for temperature).
2. Sample daily deviation using AR(1), conditioned on prior day and season.
3. Generate diurnal curve via Fourier series, adjusted for day type (e.g., cloudiness).
4. Sample 30-min values using Markov chain for cloud states and AR(1) for fluctuations.
5. Compute $P_{\text{actual}}(t)$ using sampled weather parameters and local adjustment $f_{\text{local}}(t)$.

Prospective scenarios use weather forecasts if available; otherwise, MC simulations based on historical distributions. Calibration aggregates data to 30-min intervals for consistency.

## Consumption Model
### Consumption Components
Consumption includes household loads, heat-pump, and wall-box (second site). Total consumption $C(t)$ at time $t$ (15-minute interval) is:

$$C(t) = C_{\text{house}}(t) + C_{\text{hp}}(t) + C_{\text{wb}}(t)$$

Where:
- $C_{\text{house}}(t)$: Household consumption (e.g., lighting, appliances).
- $C_{\text{hp}}(t)$: Heat-pump consumption (temperature-dependent).
- $C_{\text{wb}}(t)$: Wall-box consumption (second site, one-directional).

### Stochastic Modeling
Consumption is modeled with hierarchical MC simulations:
- **Monthly:** Seasonal patterns (e.g., higher winter heating, Normal distribution: $C_m \sim \mathcal{N}(\mu_{C,m}, \sigma_{C,m}^2)$).
- **Daily:** Weekday vs. weekend profiles (e.g., higher evening loads on weekdays).
- **Hourly:** Diurnal patterns (e.g., peaks at 7–9 AM, 6–8 PM).
- **15-minute:** Short-term fluctuations (e.g., Gamma distribution for appliance bursts).

For each time step $t$:
1. Sample monthly base load from historical data.
2. Adjust for daily type (weekday/weekend).
3. Sample hourly and 15-minute loads, using Markov models for transitions.
4. Add site-specific components (e.g., heat-pump scales with temperature, wall-box with EV charging patterns).

## Bayesian Calibration
### Parameters to Calibrate
- **Production:** $\eta$ (efficiency), $\beta$ (temperature coefficient), weather distribution parameters ($\mu_m$, $\sigma_m$, Fourier coefficients, AR parameters, fog factors).
- **Consumption:** Base load means/variances ($\mu_{C,m}$, $\sigma_{C,m}$), scaling factors for heat-pump/wall-box.

### Bayesian Framework
- **Prior:** Informative priors from literature (e.g., $\eta \sim \mathcal{N}(0.17, 0.02^2)$, $\beta \sim \mathcal{N}(0.004, 0.001^2)$).
- **Likelihood:** Gaussian error between simulated and observed data:

$$L(\theta \mid D) = \prod_t \mathcal{N}(P_{\text{act}}(t; \theta) - P_{\text{obs}}(t), \sigma^2) \cdot \mathcal{N}(C(t; \theta) - C_{\text{obs}}(t), \sigma^2)$$

Where $D$ is historic data (production, consumption), $\theta$ is the parameter vector.
- **Posterior:** Sample using MCMC (e.g., Metropolis-Hastings):

$$p(\theta \mid D) \propto L(\theta \mid D) \cdot p(\theta)$$

### Calibration Process
1. Load historic data (2018–2025 for site 1, 2021–2025 for site 2).
2. Run MC simulations with initial parameters.
3. Compute likelihood comparing simulated $P_{\text{act}}(t)$, $C(t)$ to observed data.
4. Update parameters via MCMC, iterating until convergence.
5. Output: Posterior means for $\theta$, error metrics (e.g., RMSE).

## Data Sources
### PV Site Data
- **Sites:** Two sites in Maur (8124), operational since October 2018 and September 2021.
- **Data:** 15-minute intervals, including:
  - Timestamp, battery initial load, charging, discharging, PV production, household consumption, wall-box consumption (site 2), grid feed-in, grid retrieval.
- **Format:** Assumed CSV or similar time-series format.

### MeteoSwiss Data
- **Source:** `ch.meteoschweiz.ogd-smn-tower` API, Zurich station (`ZUR`).
- **Data:** 10-minute intervals since 2000, including:
  - Air temperature (`tre200s0`), dew point, relative humidity, wind speed/direction, global radiation (`gre000s0`), sunshine duration.
- **Resolution:** Aggregate to 30-minute intervals for alignment with PV data.

### Idealized Production
- **Source:** Pre-computed algorithm using roof geometry (lat/long, orientation, slope) and installed kWp, accounting for sun-roof angle cosine.
- **Output:** Idealized production (kWh) at 15-minute intervals, neglecting weather effects.

## Next Steps
- **Review:** Validate model assumptions, distributions, and equations, particularly fog adjustments and autocorrelation.
- **Implementation:** Develop C# code for MC simulations and Bayesian calibration, using libraries like `Math.NET` or `Accord.NET`. See Appendix for pseudocode.
- **Data Integration:** Link with existing MeteoSwiss API and PV data processing.

## Appendix: Weather Simulation Pseudocode
### Weather Parameter Simulation (GHI, Temperature)
```plaintext
# Inputs: MeteoSwiss data (2000–2025, daily averages), idealized production
# Output: Simulated GHI, temperature for each 30-min interval

1. Preprocess Data:
   - Aggregate 10-min MeteoSwiss data (e.g., gre000s0, tre200s0) to daily means.
   - Optionally detrend for long-term shifts (e.g., global warming).

2. Fit Seasonal Fourier Series:
   For each parameter (GHI, temp):
     - Compute daily means (2000–2025).
     - Fit 4th-order Fourier series (least squares):
       mu(t) = a0 + sum_{k=1}^4 [ ak * cos(2πkt/365) + bk * sin(2πkt/365) ]
     - Store coefficients a0, ak, bk.

3. Fit Daily Autocorrelation (AR(1)):
   For each parameter, per season (e.g., winter, summer):
     - Compute residuals: X_t = observed - mu(t).
     - Estimate phi, sigma_epsilon via Yule-Walker or MLE.
     - Check seasonal differences in ACF/PACF.

4. Fit Diurnal Fourier Series:
   For each parameter, per season/month:
     - Compute hourly means from 10-min data.
     - Fit 3rd-order Fourier series:
       G_diurnal(h) = c0 + sum_{k=1}^3 [ ck * cos(2πkh/24) + dk * sin(2πkh/24) ]
     - Store coefficients c0, ck, dk.

5. Fit Short-Term Markov Chain (Cloudiness):
   - Define states: clear, partly cloudy, overcast (based on gre000s0 thresholds).
   - Estimate transition matrix P per season, conditioned on daily GHI.
   - Map states to f_GHI(t) variance (e.g., high variance for partly cloudy).

6. Simulate Weather for Time t (30-min):
   For each day in simulation period:
     - Get day of year (t_day).
     - Compute seasonal mean: mu(t_day) from Fourier.
     - Sample daily deviation: X_t = phi * X_{t-1} + N(0, sigma_epsilon).
     - Compute daily value: param_day = mu(t_day) + X_t.
     - For each 30-min interval (daytime only):
       - Compute diurnal mean from Fourier series.
       - Sample cloud state via Markov chain, conditioned on param_day.
       - Sample short-term deviation: G_t = phi_short * G_{t-1} + N(0, sigma_short).
       - Adjust GHI with f_local(t) (fog), estimated from observed vs. regional production.
     - Output: G_act(t), T(t).

7. Compute P_actual(t):
   - Combine G_act(t), T(t), etc., with idealized production using weather effect equations.
   - Apply f_local(t) for Maur-specific fog (calibrated via Bayesian comparison).
```


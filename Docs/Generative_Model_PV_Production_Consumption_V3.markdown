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
1. **Dynamic Horizon Profile (Implemented)**  
   Model horizon elevation as a function of azimuth using Digital Elevation Model (DEM) data. For each 15-min timestep:  
   - Compute sun position (azimuth/elevation via NodaTime).  
   - Set `f_local(t) = 0` if sun_elev < horizon_elev(azimuth), else 1 (or fractional for partial shading).  
   - **Automation**: Query Google Elevation API for radial elevation profiles (61 azimuths at 5° steps in [-150°, 150°], 50 km rays, ~30m res). Cache as lookup table in `horizon_profiles.json`.  
   - **Pros**: Precise, automates terrain effects, supports per-timestep shading. **Cons**: API rate limits (mitigated by caching). **Error**: ~85% within ±10 min vs. manual times.

2. **Interpolated Sunrise/Sunset Times (Obsolete)**  
   Sample terrain-adjusted sunrise/sunset times (e.g., 4 days/month: 1st, 8th, 15th, 23rd) via APIs or manual Google Earth extracts. Fit Fourier series:  
   $$t_{\text{sunrise}}(day) = a_0 + \sum_{k=1}^4 \left( a_k \cos\left(\frac{2\pi k day}{365}\right) + b_k \sin\left(\frac{2\pi k day}{365}\right) \right)$$  
   Set `f_local(t) = 0` outside sunrise–sunset.  
   - **Pros**: Simpler, low compute. **Cons**: Ignores intra-day shading, labor-intensive for manual extracts. **Error**: 5–10%.  
   - **Note**: Obsolete due to dynamic horizon profile’s automation and per-timestep accuracy.

3. **Empirical Corrections**  
   Scale `P_actual(t)` using observed production ratios from nearby sites (e.g., MeteoSwiss stations). Adjust `f_local(t)` empirically.  
   - **Pros**: No new data. **Cons**: Coarse, assumes similar microclimate.

#### Implementation
- **Data**: Google Elevation API (`https://maps.googleapis.com/maps/api/elevation/json`) for horizon profiles; cached in `horizon_profiles.json`.  
- **Integration**: Extend `StationMetaImporter` with `HorizonProfile` class. Cache profiles (Maur: flat; Scuol: DEM-derived). Update `f_local(t)` in `EnergyModel.cs`.  
- **Validation**: Compare Scuol outputs vs. manual Google Earth times (e.g., Jan 15 sunrise ~08:20 local vs. flat ~07:45).  
- **Resolution**: Use 5° steps in [-150°, 150°] (61 points) for optimal balance of accuracy and API efficiency.  

#### 2.3.1 Handling API Noise with Area Averaging
The Google Elevation API (SRTM ~30m resolution) can return noisy elevation data for near-site points (<500m), causing unrealistic horizon angles (e.g., >70° due to spikes like 2671m at 166m). To mitigate this, elevations are averaged over a circular area (default 10m diameter) around each ray point, using 5 samples (center + 4 points at radius D/2). Outliers (>1000m from median) are filtered, and points within 500m are skipped (`minDistKm=0.5`). This yields stable horizon angles (~3–22° for Scuol), validated against manual Google Earth sunrise times (e.g., ~08:20 local on Jan 15 vs. flat ~07:45).
- **Implementation**: `HorizonProfile.GetAverageElevationAsync` queries multiple points per ray location, integrated into `StationMetaImporter`.
- **Parameters**: Configurable `diameterKm` (default 0.01 km), `minDistKm` (0.5 km), `numPoints` (50).
- **Validation**: Comparison with manual Google Earth times confirms ~85% of sunrise/sunset differences within ±10 minutes.

#### 2.3.2 Validation of Horizon-Based Sunrise/Sunset Times
The dynamic horizon profile approach (Approach 1) was validated against manual Google Earth sunrise/sunset times (Approach 2) for Scuol (46.833°N, 10.283°E) on four days per month (1st, 8th, 15th, 23rd) in 2025. Initial validation used 10° steps (-180° to 180°), yielding average differences of +0.81 min (sunrise) and +5.00 min (sunset), with ~75% within ±10 min. Refinement to 5° steps in [-150°, 150°] (61 points) with a fallback to nearest azimuth angles (e.g., -150°/150°) for out-of-range sun azimuths improved accuracy to -2.94 min (sunrise) and +3.71 min (sunset), with ~85% within ±10 min. Winter outliers (e.g., Jan 23: -25 min, Nov 15: -23 min) are attributed to fine ridge details (<5°) but have negligible PV impact due to snow-covered panels in winter. The [-150°, 150°] range covers all relevant sun azimuths, outperforming the coarser 10° resolution.

- **Comparison with Manual Times**: Approach 1 automates horizon modeling with high accuracy (~85% within ±10 min), supporting per-timestep `f_local(t)` calculations. Manual times (Approach 2) are reliable but labor-intensive and limited to daily sunrise/sunset, unsuitable for intra-day shading. Approach 1’s automation and granularity make it superior, rendering Approach 2 obsolete.
- **Resolution Impact**: The 10° (-180° to 180°) profile missed fine ridge details, leading to larger outliers (e.g., Nov 23 sunrise: +33 min). The 5° [-150°, 150°] profile captures these better (e.g., Nov 23: -7 min), with sufficient coverage for sun azimuths (±120°). To assess resolution benefits, computed elevations at 5° steps (-145°, -135°, ..., 145°) were compared to interpolated elevations from 10° steps (-150°, -140°, ..., 150°). The average difference was +0.10°, with a range of -1.54° to +1.84°. Larger differences (e.g., 15°: +1.84°, -5°: -1.54°) indicate 5° steps capture local ridge features missed by 10° interpolation, particularly in sunrise azimuths (30–90°). However, the small average difference suggests 10° interpolation is a viable fallback if API limits are a concern, though 5° is preferred for accuracy. Finer steps (e.g., 2° in [30°, 90°]) are unnecessary due to minimal PV impact in winter.
- **Conclusion**: The 5° [-150°, 150°] profile is optimal, balancing accuracy and API efficiency. Integration into `EnergyModel.cs` for per-timestep `f_local(t)` is recommended, with flat-horizon logic for Maur (8124).
- **Next Steps**: Integrate into `EnergyModel.cs`; implement flat-horizon logic for Maur; explore Sonnendach API for roof geometry; validate with historical PV production data.

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
- **Implementation**: Integrate `HorizonProfile` into `EnergyModel.cs` for per-timestep `f_local(t)` using 5° [-150°, 150°] angles.
- **Maur**: Implement flat-horizon logic for Maur (8124) in `HorizonProfile`.
- **Data Integration**: Link with Sonnendach API for roof geometry data.
- **Validation**: Finalize with historical PV production data to confirm model accuracy.

## Appendix A: Weather Simulation Pseudocode
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

## Appendix B: Comparison of Computed vs. Manual Sunrise/Sunset Times and Elevation Resolution
The dynamic horizon profile approach (Approach 1) was compared to manual Google Earth sunrise/sunset times (Approach 2) for Scuol (46.833°N, 10.283°E) on four days per month (1st, 8th, 15th, 23rd) in 2025. Computed times used horizon angles every 5° in [-150°, 150°], with linear interpolation to estimate sunrise/sunset in 10-minute steps. Manual times were extracted visually from Google Earth. All times are in UTC.

| Month | Day | Computed Sunrise (UTC) | Manual Sunrise (UTC) | Diff Sunrise (min) | Computed Sunset (UTC) | Manual Sunset (UTC) | Diff Sunset (min) |
|-------|-----|------------------------|----------------------|--------------------|-----------------------|---------------------|-------------------|
| 1     | 1   | 09:35                  | 09:40                | -5                 | 14:35                 | 14:35               | 0                 |
| 1     | 8   | 09:35                  | 09:38                | -3                 | 14:45                 | 14:38               | 7                 |
| 1     | 15  | 09:35                  | 09:37                | -2                 | 14:54                 | 14:58               | -4                |
| 1     | 23  | 08:45                  | 09:10                | -25                | 15:04                 | 15:12               | -8                |
| 2     | 1   | 08:45                  | 08:39                | 6                  | 15:34                 | 15:28               | 6                 |
| 2     | 8   | 08:45                  | 08:37                | 8                  | 15:45                 | 15:37               | 8                 |
| 2     | 15  | 08:35                  | 08:33                | 2                  | 16:04                 | 15:46               | 18                |
| 2     | 23  | 08:15                  | 08:05                | 10                 | 16:15                 | 15:54               | 21                |
| 3     | 1   | 07:35                  | 07:32                | 3                  | 16:25                 | 15:59               | 26                |
| 3     | 8   | 07:15                  | 07:20                | -5                 | 16:25                 | 16:07               | 18                |
| 3     | 15  | 06:55                  | 06:57                | -2                 | 16:25                 | 16:14               | 11                |
| 3     | 23  | 06:25                  | 06:25                | 0                  | 16:25                 | 16:21               | 4                 |
| 4     | 1   | 06:04                  | 06:01                | 3                  | 16:25                 | 16:27               | -2                |
| 4     | 8   | 05:45                  | 05:54                | -9                 | 16:34                 | 16:32               | 2                 |
| 4     | 15  | 05:25                  | 05:32                | -7                 | 16:34                 | 16:37               | -3                |
| 4     | 23  | 05:15                  | 05:12                | 3                  | 16:45                 | 16:41               | 4                 |
| 5     | 1   | 04:45                  | 04:48                | -3                 | 16:45                 | 16:47               | -2                |
| 5     | 8   | 04:34                  | 04:36                | -2                 | 16:55                 | 16:52               | 3                 |
| 5     | 15  | 04:15                  | 04:20                | -5                 | 16:55                 | 16:55               | 0                 |
| 5     | 23  | 04:04                  | 04:04                | 0                  | 17:04                 | 17:00               | 4                 |
| 6     | 1   | 04:04                  | 03:59                | 5                  | 17:04                 | 17:05               | -1                |
| 6     | 8   | 03:55                  | 03:58                | -3                 | 17:04                 | 17:09               | -5                |
| 6     | 15  | 03:55                  | 04:00                | -5                 | 17:15                 | 17:11               | 4                 |
| 6     | 23  | 03:55                  | 04:00                | -5                 | 17:15                 | 17:14               | 1                 |
| 7     | 1   | 04:04                  | 04:01                | 3                  | 17:15                 | 17:14               | 1                 |
| 7     | 8   | 04:04                  | 04:05                | -1                 | 17:15                 | 17:13               | 2                 |
| 7     | 15  | 04:15                  | 04:08                | 7                  | 17:15                 | 17:12               | 3                 |
| 7     | 23  | 04:15                  | 04:27                | -12                | 17:04                 | 17:08               | -4                |
| 8     | 1   | 04:34                  | 04:41                | -7                 | 17:04                 | 17:03               | 1                 |
| 8     | 8   | 04:45                  | 04:53                | -8                 | 16:55                 | 16:58               | -3                |
| 8     | 15  | 04:55                  | 05:00                | -5                 | 16:55                 | 16:51               | 4                 |
| 8     | 23  | 05:25                  | 05:24                | 1                  | 16:45                 | 16:43               | 2                 |
| 9     | 1   | 05:34                  | 05:45                | -11                | 16:34                 | 16:32               | 2                 |
| 9     | 8   | 05:55                  | 05:51                | 4                  | 16:25                 | 16:23               | 2                 |
| 9     | 15  | 05:55                  | 05:59                | -4                 | 16:15                 | 16:14               | 1                 |
| 9     | 23  | 06:15                  | 06:18                | -3                 | 16:04                 | 16:03               | 1                 |
| 10    | 1   | 06:45                  | 06:53                | -8                 | 16:04                 | 15:50               | 14                |
| 10    | 8   | 07:05                  | 07:01                | 4                  | 15:55                 | 15:38               | 17                |
| 10    | 15  | 07:15                  | 07:30                | -15                | 15:55                 | 15:28               | 27                |
| 10    | 23  | 07:54                  | 07:58                | -4                 | 15:34                 | 15:18               | 16                |
| 11    | 1   | 08:05                  | 08:06                | -1                 | 15:15                 | 15:09               | 6                 |
| 11    | 8   | 08:15                  | 08:10                | 5                  | 15:04                 | 14:59               | 5                 |
| 11    | 15  | 08:15                  | 08:38                | -23                | 14:45                 | 14:50               | -5                |
| 11    | 23  | 08:35                  | 08:42                | -7                 | 14:35                 | 14:37               | -2                |
| 12    | 1   | 09:24                  | 09:19                | 5                  | 14:24                 | 14:26               | -2                |
| 12    | 8   | 09:24                  | 09:25                | -1                 | 14:24                 | 14:25               | -1                |
| 12    | 15  | 09:35                  | 09:32                | 3                  | 14:24                 | 14:24               | 0                 |
| 12    | 23  | 09:35                  | 09:37                | -2                 | 14:24                 | 14:24               | 0                 |

### Elevation Resolution Comparison
To assess the benefit of 5° resolution, computed horizon elevations at 5° steps (-145°, -135°, ..., 145°) were compared to elevations interpolated from 10° steps (-150°, -140°, ..., 150°). The average difference was +0.10°, with a range of -1.54° to +1.84°. Larger differences (e.g., 15°: +1.84°, -5°: -1.54°) indicate 5° steps capture local ridge features missed by 10° interpolation, particularly in sunrise azimuths (30–90°). This supports the preference for 5° resolution, though 10° interpolation is a viable fallback if API limits are a concern.

| Azimuth (South=0°) | Computed Elevation (5° steps) | Interpolated Elevation (from 10° steps) | Difference (Computed - Interpolated) |
|--------------------|------------------------------|-----------------------------------------|-------------------------------------|
| -145°              | 9.76°                        | 9.68°                                   | +0.08°                              |
| -135°              | 7.06°                        | 7.24°                                   | -0.18°                              |
| -125°              | 4.62°                        | 4.60°                                   | +0.02°                              |
| -115°              | 3.28°                        | 4.28°                                   | -1.00°                              |
| -105°              | 5.29°                        | 6.41°                                   | -1.12°                              |
| -95°               | 8.28°                        | 8.42°                                   | -0.14°                              |
| -85°               | 8.87°                        | 9.70°                                   | -0.83°                              |
| -75°               | 11.66°                       | 11.90°                                  | -0.24°                              |
| -65°               | 13.92°                       | 14.06°                                  | -0.14°                              |
| -55°               | 16.88°                       | 16.50°                                  | +0.38°                              |
| -45°               | 16.75°                       | 16.00°                                  | +0.75°                              |
| -35°               | 15.29°                       | 15.92°                                  | -0.63°                              |
| -25°               | 15.77°                       | 16.53°                                  | -0.76°                              |
| -15°               | 12.24°                       | 11.26°                                  | +0.98°                              |
| -5°                | 6.17°                        | 7.71°                                   | -1.54°                              |
| 5°                 | 11.32°                       | 11.28°                                  | +0.04°                              |
| 15°                | 15.26°                       | 13.42°                                  | +1.84°                              |
| 25°                | 12.15°                       | 11.44°                                  | +0.71°                              |
| 35°                | 7.87°                        | 7.60°                                   | +0.27°                              |
| 45°                | 7.64°                        | 7.08°                                   | +0.56°                              |
| 55°                | 6.88°                        | 5.84°                                   | +1.04°                              |
| 65°                | 5.37°                        | 4.84°                                   | +0.53°                              |
| 75°                | 6.90°                        | 7.20°                                   | -0.30°                              |
| 85°                | 12.46°                       | 11.84°                                  | +0.62°                              |
| 95°                | 14.93°                       | 14.88°                                  | +0.05°                              |
| 105°               | 17.16°                       | 17.10°                                  | +0.06°                              |
| 115°               | 18.78°                       | 18.78°                                  | 0.00°                               |
| 125°               | 20.85°                       | 20.50°                                  | +0.35°                              |
| 135°               | 20.33°                       | 20.02°                                  | +0.31°                              |
| 145°               | 18.11°                       | 18.18°                                  | -0.07°                              |
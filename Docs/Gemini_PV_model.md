# PV Site Generative Model: Theoretical to Effective Production

## 1. Model Objective

The goal of the generative model is to refine the **"theoretical"** energy production profile (based on installed power and geometry) into an **"effective"** production profile (actual power output) by accounting for real-world atmospheric and thermal losses.

This is achieved by using the following core weather inputs from your 15-minute data:
* **Global Radiation (GHI) [W/m²]** $\rightarrow$ Primary input for solar resource attenuation.
* **Temperature ($T_{\text{ambient}}$) [°C]** $\rightarrow$ Drives thermal performance loss.
* **Wind Speed ($W$) [km/h]** $\rightarrow$ Drives the thermal cooling effect.

---

## 2. Implementation Note (C# / .NET)

* There is no widely adopted, direct C# equivalent to the PVLIB Python library.
* **Recommendation:** Directly implement the physics-based algebraic functions (like the Faiman thermal model and the power conversion formula) into C# methods. These formulas are straightforward to port.

---

## 3. The Core Conversion Function (DC Power Output)

The electrical power output ($P_{\text{eff}}$ in $\text{kW}$) is modeled as a product of the irradiance on the panel plane ($G_{\text{POA}}$) and the operating cell temperature ($T_{\text{cell}}$).

$$\mathbf{P}_{\text{eff}} = \mathbf{G}_{\text{POA}} \times \mathbf{P}_{\text{inst}} \times \boldsymbol{\eta}_{\text{sys}} \times [1 + \boldsymbol{\gamma} \times (T_{\text{cell}} - T_{\text{STC}})]$$

| Parameter | Unit | Description |
| :--- | :--- | :--- |
| $P_{\text{eff}}$ | $\text{kW}$ | **Effective AC Power Output** (The final prediction). |
| $G_{\text{POA}}$ | $\text{W}/\text{m}^2$ | **Plane of Array Irradiance** (Output of the Transposition Model). |
| $P_{\text{inst}}$ | $\text{kWp}$ | **Installed DC Power** (Rated capacity at STC). |
| $\eta_{\text{sys}}$ | (Unitless) | **System Efficiency Factor** (Inverter, wiring, soiling losses; **must be calibrated**). |
| $\gamma$ | $/^\circ\text{C}$ | **Temperature Coefficient** (Negative value, specific to PV module; **must be calibrated**). |
| $T_{\text{cell}}$ | $^\circ\text{C}$ | **Cell Operating Temperature** (Output of the Thermal Model). |
| $T_{\text{STC}}$ | $25^\circ\text{C}$ | **Standard Test Condition Temperature.** |

---

## 4. Pillar I: Solar Resource Transposition ($G_{\text{POA}}$)

The measured Global Horizontal Irradiance (GHI) must be converted to Plane of Array Irradiance ($G_{\text{POA}}$) using a transposition model that incorporates your known roof geometry (tilt and azimuth).

### Model Steps:

1.  **GHI Decomposition:** Decompose the measured GHI into its constituent components: **Direct Normal Irradiance ($\text{DNI}$)** and **Diffuse Horizontal Irradiance ($\text{DHI}$)**. (Use a model like the **Erbs Model**).
2.  **Transposition:** Combine $\text{DNI}$, $\text{DHI}$, and the ground reflected component, adjusting for the panel angle. The **Perez Model** or **HDKR Model** are industry standards for accurate transposition.

---

## 5. Pillar II: Thermal Loss Model ($T_{\text{cell}}$)

Thermal effects cause a significant reduction in PV efficiency. The operating cell temperature ($T_{\text{cell}}$) is estimated using the **Faiman Thermal Model**:

$$T_{\text{cell}} = T_{\text{ambient}} + \frac{G_{\text{POA}}}{\mathbf{U}_{\mathbf{0}} + \mathbf{U}_{\mathbf{1}} \times W}$$

| Parameter | Unit | Description |
| :--- | :--- | :--- |
| $T_{\text{cell}}$ | $^\circ\text{C}$ | **Cell Operating Temperature.** |
| $T_{\text{ambient}}$ | $^\circ\text{C}$ | **Measured Ambient Temperature** (from weather data). |
| $G_{\text{POA}}$ | $\text{W}/\text{m}^2$ | **Plane of Array Irradiance** (from Transposition Model). |
| $W$ | $\text{km}/\text{h}$ | **Measured Wind Speed** (use 2m or 10m data). |
| $\mathbf{U}_{\mathbf{0}}, \mathbf{U}_{\mathbf{1}}$ | $(\text{W}/\text{m}^2\cdot\text{K})$ | **Heat Loss Coefficients** (**Must be calibrated** using historical data). |

---

## 6. Model Calibration

The parameters $\boldsymbol{\eta}_{\text{sys}}$, $\boldsymbol{\gamma}$, $\mathbf{U}_{\mathbf{0}}$, and $\mathbf{U}_{\mathbf{1}}$ are specific to your installation and must be **calibrated** against your 7 years of historical production data to finalize your generative model. Use standard optimization or regression techniques (e.g., Least Squares fitting) for this step.
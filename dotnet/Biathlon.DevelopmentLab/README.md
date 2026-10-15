# M15.6 shared development study

Run from the repository root after building the project in Release:

```powershell
dotnet dotnet/Biathlon.DevelopmentLab/bin/Release/net8.0/Biathlon.DevelopmentLab.dll . 12 15 junior
```

Scenarios: `control`, `junior`, `national`, `world_cup`, `catalog_low`,
`catalog_high`. Twelve fixed seeds ×30 entrants give360 trajectories per
scenario. The five authored starting scenarios total1800; the common-ability
control contributes360 additional trajectories. Every trajectory is observed
for15 annual boundaries, with shared production training/adaptation/ageing
until its individual retirement age. Afterwards it is frozen. This is a
controlled fixed-cohort experiment, not a simulation of one complete changing
career world. World retention/generation is checked separately in native tests.

Adaptive AI, repeated intensity and a balanced rotation use the same actual
training implementation. Race load is the authored aggregate weekly cost;
**no authoritative race, result, win, podium or geometric shot is fabricated**.
Use `Biathlon.CareerBalanceLab` for actual race competition evidence.

JSON outputs retain per-athlete starting/peak/current ratings, actual ages,
hidden style/band for developer analysis, retirement, seasonal series and
policy hashes. Stagnant/declining-season counts include active observations
only; frozen retirees are separately identified and excluded from active
seasonal means. Full fixed-cohort means are explicitly labelled. OVR95+ is
rare in this bounded sample and is not guaranteed by a starting preset.

Results live under `artifacts/m15-6/development-study[-scenario]`. Reviewed
copies are in `docs/quality/m15-6/development-study/`; `initial-slow-control`
retains the original underdeveloping calibration and is not the final policy.

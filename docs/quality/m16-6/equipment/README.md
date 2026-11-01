# M16.6.2 — Equipment, economy and shop

2026-11-01. Focused qualification complete; full assembly regression running. M16.6 OPEN.

Eight skis / four rifles use the original 8% envelope and 3% snow affinity limit.
No target, geometric hit rule, athlete power, race length or training formula changes.
Cold, fresh, ice and wet snow have different fastest models. The measured rolling
three-lap matrix covers all 32 ski/snow pairs and compares stat50 with stat80.
Gear saves seconds, broad skills tens of seconds. See `m166-ski-matrix.csv`.

The two-season spreadsheet is a financial scenario calculation, not simulated
rank evidence. All three calendars have 16 events/season. Weak rank sequence:
27/23/18/29/21/26; middle: 9/7/8/3/10/6; strong: 3/1/2/1/4/2. The sample buys
camp+recovery when affordable every third race, with free training/rest available.
Starting money500; early skis700/900; specialist skis3000–6000; rifles2600–5200;
camp250; recovery75. New prizes have regional/national/international scaling
65%/100%/150%. The calculation deliberately shows that weak juniors who repeatedly
buy services save slowly: their starter gear remains fully competitive, and
skipping optional services makes the early pair attainable after four races.
Premium gear is a savings choice, not a prerequisite to participate.

Save4 migration adds neutral rifle ownership/selection only, including world pool.
Archived v1/v2/v3 fixtures and hashes remain untouched. Rifle ergonomics affects
setup/exit; handling affects bolt/settling; recoil affects post-shot angular recoil.
New replay metadata freezes all resolved ski/rifle/handling data. Missing metadata
uses original three skis and neutral handling. AI pays the same prices and keeps
a325-unit service reserve; its choice reuses SkiEquipmentSystem, with no career RNG.

Initial dotnet qualification:72/72 core,34/34 replay/neutral trace/front-end regressions.
The initial30-field attempt hit an incorrectly short90-second external watchdog;
rerun uses the existing15-minute test timeout. It is not counted as a pass.
Full30 field:PASS,2 complete races/300 shots, exact result/event equality and Pursuit qualification.
Native EditMode30/30 passed. Native first batch:12/14 passed. Both Mass Start
fixtures omitted their recorded contact-policy version (created with26, default
metadata means25); fixtures now declare the actual policy explicitly. Final
Mass Start rerun and standalone build receipts pending. Shop recapture1/1 passed
after waiting for the real menu entrance transition; original3-frame captures
were premature background-only images and are replaced.

Final Mass Start2/2 PASS including forward/backward seek, exact poses and v2
snapshot bytes. Native Play coverage14 distinct cases with corrected receipts;
shop repeated separately1/1. Console0errors/exceptions/asserts. Build Succeeded,
0errors/520existing warnings,427248296bytes,28.237s. Actual standalone startup
PASS/exit0,2560×1440FullScreenWindow,drawn bib20,personal3–2–1–0 and owner profiles
unchanged. Optional advanced navigation/FPS benchmark was not enabled.
Full .NET assembly run remains in progress; its final receipt will be appended.

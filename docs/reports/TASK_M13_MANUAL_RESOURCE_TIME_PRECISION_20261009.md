# M13 actual-time range resource ordering — 2026-10-09

Verified isolated fix; M13 / WP175 / WP177 / WP178 remain OPEN. WP176 PASS.
Production host remains lateral-v1. No Windows owner build.

An accepted physical contact piece can be shorter than one billionth of a fixed
tick. The scoped arrival reader incorrectly required every physical piece to
remain nonempty after retiming to resource ordering fractions. That threw after
a fully proved tick, even for a piece with no credited route change.

Scoped resource arrival now interpolates the line crossing on the actual complete
physical receipt BEFORE applying ceiling to its integer ordering fraction. Tiny
prefixes/suffixes and all travel/route/source/clock validation remain intact.
This changes resource ordering only; it grants no collision, course, fork, mat or
clock admission. The legacy retimed motion timeline and its stricter segment
contract remain unchanged. Field-stepper context v7 captures the new policy.

Both minimized tests first failed on the actual internal receipt reader, with
real prepared two-piece world proposals. A fixture initially supplied a nominal
tiny suffix rather than the exact remaining double clock; that SourceMismatch
was corrected before the two red-capable arrival tests were rerun. No guard or
assertion was disabled. The two tests now pass in .NET and Unity EditMode.

Validation: focused arrival selection30/30 PASS. Whole manual/legacy-arrival selection
616 PASS / 2 FAIL / 0 skipped (618; 5m57s). The same pre-existing complete-field
course failures remain: Mass30 tick163 and Sprint40 tick8724. Complete independent
signed replay tests are included and remain green. Native precision2/2 PASS;
compile settled and Error/Exception Console empty after the precision refresh.

Evidence: `docs/quality/m13/field-stepper-20261008/manual-scoped-arrival-*`.
Three additional dense-field diagnosis trials remain failures and were removed:
neighbor trace163; long shared pursuit260/6642; shorter shared pursuit351/6601.
They are not production tuning or acceptance. Next dense-traffic spin-aware
forecast, real complete fields, signed player mat capture and host, then final
weather/balance/performance qualification. Owner build only after ALL M13.

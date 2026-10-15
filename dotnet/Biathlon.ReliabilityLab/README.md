# M16 production career reliability soak

Run from the repository root:

```powershell
dotnet run --project dotnet/Biathlon.ReliabilityLab -c Release -- C:/game/Biathlon/Biathlon C:/game/Biathlon/Biathlon/artifacts/m16/three-season-soak
```

The bounded study completes all 156 weekly transitions and all 48 events in three
current production seasons. It uses `FrontEndSession`, the actual profile store,
weekly training, confirmed pre-race checkpoints, shared production Woodland race
physics, geometric shooting, results publication and a fresh Continue after every
week. A fixed seed and authored training policy make the run reproducible. No
calendar week is skipped and no finish result is fabricated. Authority ticks,
shot counts and full authority result payloads are retained separately per race.

It also rejects deliberately failed training/result writes, retries the same
prepared race, cancels pre-race previews, abandons and retries once per season,
checks exactly-once result publication, verifies previous-generation backups,
checks persistent player identity/cosmetics/talent, annual archives/world bounds,
and tests explicit corrupt-primary backup recovery after the final season.

The automated controller drives all entrants. This is a core simulation and
persistence integration soak, **not** evidence of 48 rendered native races or
three seasons of human interaction. Native front-end and fault scenarios have
separate focused test fixtures.

Output directories must be fresh. `--resume` permits recovery only when the
retained source manifest and weekly evidence agree exactly with the durable save;
it never silently reruns or overwrites an earlier study. Tick budgets come from
the normal authored career race definition. Concurrent test runs mean wall time
is diagnostic only and is not an FPS/performance benchmark.

`--resume-persistence` is an intentionally narrow alternative: the retained
weekly sidecar and durable save must still match, and the **only** production
code input allowed to differ is `FileCareerProfileStore.cs`; UI text and the
device binding catalog may also differ because this AI-controlled core soak
does not interpret device input. It writes a separate continuation manifest
retaining both source identities. Physics, AI, career rules, gameplay definitions
and any other code changes forbid this mode. This supports a
verified safe retry after a storage failure without claiming previous races
were executed twice.

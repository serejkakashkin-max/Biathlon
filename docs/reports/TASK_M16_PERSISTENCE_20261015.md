# M16 WP-206–209 — Career persistence and recovery

2026-10-15. Implementation checkpoint; native Unity qualification and final milestone build are owned by the root task. The owner accepted M15.6 and explicitly directs that its approved behaviour wins conflicts with the original milestone list.

## Format and migration

The plan's historical “Save format v1” heading does not reset the product to an old format. Career `schema_version: 1` / `save_version: 3`, current appearance profiles, development, persistent rivals, stable IDs, recorded results and RNG remain authoritative. Settings have their own independently versioned store. No career payload envelope or gameplay metadata is injected on load.

`CareerSaveMigrations.Load` is the explicit, IO-free ordered migration entry point; the existing `CareerSave.FromJson` delegates to it. Its receipt names the source/target version and migration steps. The existing v1 confidence → v2, then v2 deterministic legacy appearance → v3 transformations are unchanged. Unsupported versions have a typed exception before full current-schema deserialization. A future primary or future recovery candidate is never silently downgraded to a supported older copy.

Three immutable synthetic schema fixtures under `Assets/Tests/Fixtures/CareerMigration/` represent v1/v2/v3, including a recorded result string with meaningful whitespace/escaped quotes and its fingerprint. They were generated once from the real deterministic career factory and version-specific historical field sets, rather than copied from a user's private profile. Sidecar SHA-256 files pin each input and the exact v3 output. Tests retain every ability, RNG state, allocation counter, stable identity, recorded payload and digest, and prove idempotence. Existing richer M15.6 migration/profile/world tests remain in the project.

## Save transactions and recovery

New profiles and replacements use a same-directory temporary file, `Flush(true)`, byte-for-byte readback plus full syntax/schema/identity validation, then filesystem atomic move/replace under an exclusive per-profile lock. Replacements still compare the current canonical career to the command's expected source. Stale writers do not overwrite newer progress.

Continue selects the newest career family using the original primary chronology, then checks only that same profile's primary, `.bak` and `.checkpoint`, in that order. Corrupt/missing data can fall back; denied access, IO failures and unsupported versions do not masquerade as corruption. It never silently opens a different athlete's older career. A recovery receipt reports its source, original/target save version and content hash without names, paths or save contents.

Reads preserve all career/backup/checkpoint/temp data bytes; their sole filesystem bookkeeping side effect is an exclusive `.lock` file. A subsequent valid command repairs a corrupt/missing primary without copying its corrupt bytes over the last good backup. The corrupt primary is retained with a unique `.corrupt-…` suffix. Temporary files left by a process interruption are uncommitted: they are ignored and preserved, never automatically promoted. Normal failed-operation temps are removed where possible; a cleanup failure cannot turn an already committed transaction into an apparent failure.

`HasProfile` keeps Continue available for damaged or inaccessible saves, allowing a useful error/recovery UI rather than presenting a new empty game or crashing the home page. `LastRead` clears before a failed attempt so diagnostics cannot report a stale successful recovery.

## Autosave cadence

The existing save-before-publish semantics remain: confirmed creation, successful training/rest week, equipment transaction, completed race and annual transition are persisted before changing the visible career. Draft edits, previews, race preparation, canceled races and speculative trained race snapshots do not consume or save career weeks/RNG.

`FrontEndSession.BeginRace` now invokes the optional `ICareerProfileCheckpointStore` before changing screen. The filesystem implementation saves the exact already committed `PreparedWeek.SourceJson` as an independently verified `.checkpoint`; it does not save `RaceSnapshotJson`. A failed checkpoint leaves PreRace, prepared seed/snapshot and career/disk bytes intact; retry is safe. No unsupported mid-race resume is invented. Existing in-memory stores remain source-compatible through the optional interface.

## Integrity defect found during qualification

A red three-case test found that the framework JSON serializer accepted both a missing closing root brace and an appended second JSON object. Such input formerly passed validation and could become discoverable as a new profile. The shared `Biathlon.Domain.JsonDocumentSyntax` (called through the save-only `CareerSaveJsonSyntax` adapter) now requires exactly one complete JSON object, strict numbers/strings, unique member names (including escaped equivalents), and bounded recursion before either header or current-schema deserialization. This preflight does not rewrite or normalize valid input. The settings task reuses the same strict parser without coupling its schema or filesystem store to careers. Strict UTF-8 disk decoding also prevents malformed byte sequences being silently replaced before validation. The original red result is retained.

## Verification

Release CoreCLR test build: **0 errors, 0 warnings**. Focused run: **64 passed, 0 failed, 0 skipped**, in `docs/quality/m16/persistence/core-final.trx`:

- 29 `CareerProfileReliabilityTests`: corruption/missing-primary recovery, good-backup retention, checkpoint fallback, uncommitted temps, unrelated-profile isolation, future version protection, wrong identity, stale writer, interrupted writes, verification failure, replacement/move failure, denied reads/listing, lock contention, malformed UTF-8 and retry/abandon semantics.
- 15 `CareerSaveMigrationFixtureTests`: pinned v1/v2/v3, read-without-rewrite / CAS migration backup, malformed/duplicate JSON rejection and legacy BOM/whitespace compatibility.
- 20 existing foundation, appearance migration and front-end session cases. The long actual production race/replay case is excluded from this focused run and is not claimed here; the separate WP-215 run executes actual full races.

The first 42-case run had two fixture/API expectation failures: one fabricated week edit accidentally changed a nested development record, and the stale-writer test expected an exact base `IOException` although the new error is a typed derived exception. Both assertions were corrected to test the actual valid contract, without deleting/ignoring cases. The strict-JSON run had the two genuine failures above; all are retained and now green. Native result and full soak evidence are reported separately by the root task.

## Integration API and limits

`FileCareerProfileStore.LastRead` → `CareerProfileReadInfo`: `Source`, `SourceSaveVersion`, `SaveVersion`, `WasMigrated`, `WasRecovered`, `ContentSha256`. `CareerProfileException.Failure`: `NoProfile`, `Corrupt`, `UnsupportedVersion`, `AccessDenied`, `IoFailure`, `Changed`. UI must select localized messages from these codes rather than display paths/inner exception details. `ICareerProfileFileSystem` enables repeatable failure injection and is not an alternative gameplay authority.

No simulation/AI/physics/shooting/balance parameters or deterministic race state changed. This is process-crash-safe transactional recovery on a filesystem that supports the platform's atomic replacement API; no claim is made about storage-controller hardware failure, power-loss durability of an unflushed directory entry, or external software that deliberately ignores the lock. Damaged artifacts are retained for non-destructive investigation; automatic deletion/compaction of a user's evidence is deliberately absent.

## Soak-driven checkpoint follow-up

The actual WP-215 run exposed a transient Windows replacement error on the second BeginRace at season 2/week 6, after 57 completed weeks and 16 real production races. The already valid primary/checkpoint stayed intact. A red regression reproduced the unnecessary write through real BeginRace → AbandonRace → BeginRace with an injected write failure. The checkpoint operation now revalidates the existing same-profile copy under the exclusive lock, after checking the current primary against the expected source, and reuses it only when its raw JSON is exactly equal. A changed source still requires a new verified write; a stale source still rejects. No broad retry loop, exception suppression or file deletion was added. The source freeze includes this fix.

The latest focused .NET result is **68 passed / 0 failed / 0 skipped**, 0 build errors/warnings, in `core-checkpoint-final.trx`: 33 reliability cases, 15 migration cases and the same 20 legacy cases. The extra reliability case from the root task verifies that successful publication updates CurrentSource health independently of retained LastRead recovery history. The original 64-case checkpoint remains in `core-final.trx`. Native qualification and the WP-215 full-run/resume/final-store persistence audit are reported separately, not inferred from these 68 tests.


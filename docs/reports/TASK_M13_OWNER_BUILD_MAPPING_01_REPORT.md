# TASK_M13_OWNER_BUILD_MAPPING_01

2026-10-05. Status: PASS.

The production build succeeded, but restoring the temporary PlayerSettings timing
and input-preload bytes failed with Win32 error1224 when Unity retained a read-only
memory mapping. Truncating the mapped file with WriteAllBytes was the failing
operation. The temporary timing lease now verifies its exact expected bytes,
writes a sibling temporary file, checks again for concurrent owner edits, and
uses an atomic File.Replace. The mapped reader keeps its existing complete view;
the path receives the exact original owner bytes. Concurrent independent edits
retain the existing preservation behavior. Temporary files are cleaned up.

Changed files: PlayerBuildProductionReview.cs and ProductionReviewBuildLeaseTests.cs.
The native mapped-file reproduction failed before the fix. All13 tests in the
native Editor build-lease fixture passed afterward, with no skips. The actual
WoodlandRealismReview20261005 rebuild then succeeded and the lease returned
normally. Its incremental summary reports0errors,0warnings,12.029s and356164329bytes.
The earlier full build reported496 inference shader warnings; the cached rebuild
does not establish that those existing shader warnings have been fixed.

All23 independently owned disk paths, six native owner assets, their dirty flags
and frame-timing state were verified unchanged after the final build. The
restoration of Unity's observed build-only Quality/URP serialization changes was
accepted only when the candidate recovered the captured full-file SHA256; no
blanket reset or SaveAssets was used. Full native owner-state snapshots agree.

Hidden Windows Mass30/Sprint40 startup and paid400-tick content checks pass. Hidden
rendering was suspended, so no standalone visual or GPU/FPS result is claimed.
This Editor-only correction has no deterministic race or balance impact. Native
receipts, the actual build summary and preservation evidence are packaged under
`docs/quality/m13/owner-realism-20261005/`. M13 and the remaining gameplay corrections
stay open.

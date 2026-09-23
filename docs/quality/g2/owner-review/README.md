# G2 owner review008A

Открыть `index.html`. Итоговое решение агента: **recommend G2 FAIL / more remediation needed**; владелец ещё не принимал G2.

Источник кадров — `Assets/Tests/PlayMode/G2OwnerReviewTests.cs`. В Unity MCP:

```powershell
./scripts/Invoke-UnityMcp.ps1 -Tool tests-run -Arguments @{
    testMode='PlayMode'; testAssembly='Biathlon.PlayModeTests'
    testClass='G2OwnerReviewTests'; includePassingTests=$true; includeStacktrace=$true
} -TimeoutSec 240 -OutputFile artifacts/g2-owner-review/capture-test-final.json
```

После успешного run собрать ConsoleState/compile warnings через MCP, скопировать актуальный Unity TestResults.xml в `artifacts/g2-owner-review/test-results.xml`, затем выполнить `python scripts/Build-G2OwnerReview.py` в Python с Pillow. Builder требует45stills,48нечёрных motion frames,1passed test, marker завершения,30совпадающих before/after камер и pixel-identical rewind. Он не создаёт игровые изображения синтетически. Результаты каждого нового run заменят текущий package; текущие hashes/дата относятся к2026-09-23.

Воспроизведение требует licensed local restricted crowd derivatives, отсутствующих в публичном Git; см. `docs/reports/G2_CROWD_IMPLEMENTATION.md` и import scripts. До запуска нужно сохранить открытые Unity scenes.

Локальная сохранённая сборка: `C:\game\Biathlon\Biathlon\Builds\G2OwnerReview-20260922\Windows\Biathlon.exe`. Обычный запуск владельцем → кнопка **M12 visual benchmark** справа сверху в Sprint → выбор shot/quality/light/Animate. Агент использовал только hidden checks. Полный runtime build/smoke/profile evidence — в соседнем `../lighting-a`;008A не меняет runtime. Dedicated aim captures не содержат HUD/crosshair; pose fixtures не являются полноценной гонкой.

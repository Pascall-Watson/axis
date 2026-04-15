I’m turning the migration plan into execution-ready sprints with handoff prompts you can paste into Copilot, keeping the work small enough for a novice-managed workflow and safe enough for staged rollout.

Read memory [](file:///c%3A/Users/Wayne/AppData/Roaming/Code/User/workspaceStorage/b6087fa5447da5757285f3d77c14eddd/GitHub.copilot-chat/memory-tool/memories/ODA2NzRiZTktMThjNy00ZGQxLTgwODMtNWFiMTFlOTc0MmRj/plan.md)

I’m rewriting the stored plan into sprint-sized handoffs with clear goals, acceptance checks, and paste-ready Copilot prompts so you can drive this step by step without having to manage the architecture details yourself.

Deleted memory [](file:///c%3A/Users/Wayne/AppData/Roaming/Code/User/workspaceStorage/b6087fa5447da5757285f3d77c14eddd/GitHub.copilot-chat/memory-tool/memories/ODA2NzRiZTktMThjNy00ZGQxLTgwODMtNWFiMTFlOTc0MmRj/plan.md)

Created memory file [](file:///c%3A/Users/Wayne/AppData/Roaming/Code/User/workspaceStorage/b6087fa5447da5757285f3d77c14eddd/GitHub.copilot-chat/memory-tool/memories/ODA2NzRiZTktMThjNy00ZGQxLTgwODMtNWFiMTFlOTc0MmRj/plan.md)

Use 5 sprints. That is the right size here: small enough that Copilot can complete each one, but large enough that each sprint leaves you with a meaningful working checkpoint.

## Plan: 5-Sprint Delivery Plan

The overall strategy is simple: do not rewrite the Revit and Excel logic in Python. Keep the backend in C#, extract it from the WPF viewmodel, and then build the pyRevit UI on top of that stable backend. Keep the current WPF path alive until the Python path is proven.

**Sprint 1**
Goal: freeze the baseline and make the repo safer to work on.

What gets done:
1. Verify and document the current build, stage, and smoke-test flow for Revit 2025 and 2026.
2. Improve README and pyRevit launcher diagnostics.
3. Add clear failure messages and loaded-DLL-path reporting.
4. Do not change the actual WPF-driven workflow yet.

Definition of done:
1. Build-PyRevitHybrid.ps1 still stages the extension.
2. The pyRevit button still launches the current WPF window.
3. The repo contains a clear manual smoke test for export and import.
4. A novice can follow the docs and run it.

Prompt to give Copilot:
Implement Sprint 1 only. Create a production-safe baseline for the pyRevit migration in this repo without changing the current WPF-driven workflow. Improve README and extension documentation with exact build, staging, rollback, and smoke-test steps for Revit 2025 and 2026. Improve the pyRevit launcher so failures show the attempted DLL path and clear next steps. Keep the current pyRevit launcher and C# WPF window working exactly as they do now. Do not refactor core logic, do not remove WPF code, and do not start the Python UI migration yet. Verify modified files for errors and summarize the manual smoke tests to run.

**Sprint 2**
Goal: create the C# API boundary Python will call later.

What gets done:
1. Add DTOs for schedules, standards, import previews, requests, results, warnings, and progress.
2. Extend ExcelExporterImporterInterop.cs with service-style methods.
3. Keep `ShowMainWindow(...)` working for backward compatibility.
4. Make sure the new API exposes no WPF types.
5. Resolve or suppress the MSB3277 Microsoft.Extensions.* version conflicts from the Revit 2026 build (carried over from Sprint 1 build output).

Definition of done:
1. The solution builds.
2. The old WPF launcher still works.
3. New interop methods can be called from Python without needing window objects or WPF classes.
4. No user-visible workflow change yet.

Prompt to give Copilot:
Implement Sprint 2 only. Create a clean C# backend facade for the future pyRevit Python UI while preserving the existing WPF launcher. Add DTOs and service-style interop methods for loading exportable schedules, loading standards options, inspecting an import workbook, and executing export or import requests. The new public API must not expose WPF types, dialog types, or window handles. Keep ExcelExporterImporterInterop.ShowMainWindow(...) working exactly as it does now. Do not replace the UI yet. Keep the changes minimal and production-safe, update documentation for the new interop boundary, and verify the project still builds cleanly.

**Sprint 3**
Goal: move business workflow out of `MainViewModel`.

What gets done:
1. Extract orchestration from MainViewModel.cs.
2. Move schedule discovery, standards discovery, import workbook inspection, overwrite rules, progress coordination, and result summaries into backend service classes.
3. Keep Revit transactions and EPPlus logic in C#.
4. Keep the WPF UI working by having it use the new backend services.

Definition of done:
1. The WPF UI still behaves the same.
2. The new service layer is reusable from both WPF and Python.
3. `MainViewModel` is clearly smaller and less coupled.
4. Export and import smoke tests still pass.

Prompt to give Copilot:
Implement Sprint 3 only. Refactor the current C# WPF code so that MainViewModel no longer owns most of the business workflow. Extract non-visual orchestration into backend service classes that can be reused by both the existing WPF UI and a future pyRevit Python UI. Focus on schedule discovery, standards discovery, import workbook inspection, export/import coordination, overwrite decision inputs, progress reporting, and result summaries. Keep all Revit transactions and Excel logic in C#. Preserve existing behavior and keep the WPF UI working. Do not build the Python UI in this sprint. Validate by ensuring the legacy flow still compiles and the same manual export/import smoke tests can still be run.

Validation Results (Sprint 3):
- Date: 2026-04-15
- Revit versions tested: 2025, 2026
- Scenarios run: Export flow and import flow validated on legacy WPF path
- Result: Passed

**Sprint 4**
Goal: build the real pyRevit Python UI.

What gets done:
1. Replace the launcher-only Python layer with a Python-owned export/import workflow.
2. Use the new C# facade instead of `ShowMainWindow(...)`.
3. Handle file picking, user choices, confirmations, progress, and results in Python.
4. Keep the legacy WPF path available as fallback.

Definition of done:
1. Export works from the Python UI without opening the WPF window.
2. Import works from the Python UI without opening the WPF window.
3. The legacy WPF launcher still exists as fallback.
4. The pyRevit extension still stages and loads properly.

Prompt to give Copilot:
Implement Sprint 4 only. Build a real pyRevit Python UI on top of the C# backend facade created in earlier sprints. The Python layer should own user interaction: selecting export items, selecting import files, choosing standards, confirming overwrites, showing progress, and displaying results. Do not call ShowMainWindow(...) for the new path. Keep the legacy WPF launcher available as fallback, ideally as a separate command or clearly isolated option. Use the existing loader and extension structure in axis/ExcelExporterImporter.extension. Keep the design practical and maintainable rather than visually matching the old WPF window exactly. Verify the extension still stages, loads, and calls the backend successfully.

Validation Results (Sprint 4):
- Date: 2026-04-15
- Revit versions tested: 2025, 2026
- Scenarios run: Python workflow chooser appeared; export schedules, export standards, and import workbook paths exercised; Legacy WPF fallback button still opens original C# WPF window
- Result: Passed

**Sprint 5**
Goal: make it safe for office deployment.

What gets done:
1. Add structured logging and better error handling across Python and C#.
2. Add support-oriented documentation: pilot rollout, rollback, troubleshooting, log collection.
3. Add backend-focused tests where practical.
4. Keep the Python UI as pilot or recommended path depending on verification outcome.
5. Update log4net from 2.0.3 to a version without known CVEs (NU1902/NU1904 flagged in Sprint 1 build output).

Definition of done:
1. Errors are actionable and logs are predictable.
2. The repo contains rollout and rollback instructions.
3. Regression checks are documented and repeatable.
4. The team can support it without reading the whole codebase.

Prompt to give Copilot:
Implement Sprint 5 only. Harden the new pyRevit Python UI plus C# backend for mission-critical office deployment. Add structured logging, better error handling, clearer operation summaries, and support-oriented documentation. Document pilot rollout, rollback, troubleshooting, and regression checks. Add automated backend tests where practical, and clearly document manual validation where automation is not feasible. Do not remove the legacy WPF fallback unless the repo already has strong verification and the changes are clearly production-ready. Keep the final result conservative, supportable, and easy for non-expert maintainers to operate.

## How You Should Run This

Do not ask Copilot to do all 5 sprints at once. Give it one sprint, let it finish, test that sprint, then move to the next.

Your working loop should be:
1. Paste one sprint prompt.
2. Let Copilot make the changes.
3. Run the build and your smoke tests.
4. If that sprint works, move to the next one.
5. If it does not, keep Copilot on that same sprint until it is stable.

## My recommendation

Start with Sprint 1 exactly as written. Sprint 1 is low risk and gives you a much safer base for the harder refactor work.

I also saved this sprint plan into session memory so we can keep refining it. If you want, the next useful step is:
1. I turn Sprint 1 into an even more detailed checklist you can use line by line with Copilot.
2. I turn Sprint 2 into the exact C# facade design before you implement it.
3. I write you a “how to supervise Copilot” guide so you know when to accept or reject its changes.
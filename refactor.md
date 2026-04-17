Created memory file [](file:///c%3A/Users/Wayne/AppData/Roaming/Code/User/workspaceStorage/b6087fa5447da5757285f3d77c14eddd/GitHub.copilot-chat/memory-tool/memories/MWEwMGVmNGUtYTc5Zi00OTExLWE5OTMtYzQ5NmI0ZjliMGU1/plan.md)

Now I'll create a comprehensive plan. First, let me clarify a few implementation details: 

Analyzing your answers...



Fetched 4 resources

Now I have the critical information needed. Let me update the session plan with targets frameworks and prepare the comprehensive implementation plan: 

Updated memory file [](file:///c%3A/Users/Wayne/AppData/Roaming/Code/User/workspaceStorage/b6087fa5447da5757285f3d77c14eddd/GitHub.copilot-chat/memory-tool/memories/MWEwMGVmNGUtYTc5Zi00OTExLWE5OTMtYzQ5NmI0ZjliMGU1/plan.md)

Now I'll generate the comprehensive implementation plan based on all the research:

## Plan: Multi-Version Revit Support (2024-2027 Only)

**TL;DR:** Migrate from single `.csproj` with build properties to the article's approach: abstract base `.csproj` + four version-specific `.csproj` files (one per version). This provides cleaner IDE support and IntelliSense. Update conditional symbols for 2024 (new), remove pre-2024 code blocks, and handle .NET Framework/Core split via version-specific `obj` folders and target frameworks.

---

## Steps

**Phase 1: Refactor Project Structure (Parallel Setup)**

1. Create base `.csproj` file (`ExcelExporterImporter.base.csproj`)
   - Move all common properties (RootNamespace, PlatformTarget, DefineConstants, references via `$(RevitYear)` placeholder)
   - Add `obj/` folder exclusion for compilation (as per article)
   - Add dynamic Revit API reference paths: `dependencies\$(RevitYear)\RevitAPI.dll`

2. Create `.csproj` paths file (`ExcelExporterImporter.paths.csproj`)
   - Define version-specific intermediate output paths to separate .NET Framework 4.8.1 build artifacts (2024) from .NET 8.0 artifacts (2025-2027)
   - Set `MSBuildProjectExtensionsPath`, `BaseIntermediateOutputPath`, `IntermediateOutputPath` to `obj\$(RevitYear)\`

3. Create four version-specific `.csproj` files (_one per version_):
   - `ExcelExporterImporter.2024.csproj`: `<RevitYear>2024</RevitYear>`, `<TargetFramework>net481</TargetFramework>`
   - `ExcelExporterImporter.2025.csproj`: `<RevitYear>2025</RevitYear>`, `<TargetFramework>net8.0</TargetFramework>`
   - `ExcelExporterImporter.2026.csproj`: `<RevitYear>2026</RevitYear>`, `<TargetFramework>net8.0</TargetFramework>`
   - `ExcelExporterImporter.2027.csproj`: `<RevitYear>2027</RevitYear>`, `<TargetFramework>net10.0</TargetFramework>`
   - Each imports: `ExcelExporterImporter.paths.csproj`, then `ExcelExporterImporter.base.csproj`

4. Remove old `ExcelExporterImporter.csproj` from the solution; add the four new `.csproj` files
   - Verify all four projects build successfully with correct frameworks and output paths

---

**Phase 2: Update Conditional Compilation (Code Changes)**

5. Update DefineConstants in base `.csproj` 
   - Change from `REVIT$(RevitVersion)` → keep as `REVIT$(RevitYear)` for consistency
   - Results in: `REVIT2024`, `REVIT2025`, `REVIT2026`, `REVIT2027`

6. **Remove legacy conditional blocks** in source files (pre-2024 code):
   - RevitUtilities.cs: Remove `#if REVIT2016 || REVIT2017 || REVIT2018 || REVIT2019 || REVIT2020 || REVIT2021` block (WallFoundation handling)
   - StandardsExporter.cs: Remove all `#if REVIT2017 || REVIT2018 || REVIT2019 || REVIT2020 || REVIT2021` blocks for parameter iteration and shared parameter handling (3 blocks total: lines 391-435, 464-471, 499-511)
   - Keep code that was in the `#else` block as the new default (2024+ behavior)

7. **Add version-specific code for 2024-2027 API changes** (if applicable):
   - ElementId: Add `#if !REVIT2024` conditional for `.Value` property usage (2024 uses `.IntegerValue`)
   - Other 2024-2026 API deprecations: Mark deprecated calls with `#if REVIT2024` if compatibility needed across versions
   - .NET 10 features (2027): Add `#if REVIT2027` for any .NET 10–specific APIs if used

---

**Phase 3: Build Scripts & Dependency Management**

8. Update Build-PyRevitHybrid.ps1
   - Change default parameter: `[string[]]$RevitVersions = @("2024", "2025", "2026", "2027")`
   - Update project reference loop to build `*.2024.csproj`, `*.2025.csproj`, `*.2026.csproj`, `*.2027.csproj` instead of single `.csproj`
   - Verify output paths for version-specific DLLs: Revit2024, `...Revit2025/`, etc.

9. **Acquire Revit 2024 SDK**:
   - Copy Revit 2024 API DLLs to `dependencies/2024/` folder structure (AdWindows.dll, RevitAPI.dll, RevitAPIUI.dll)
   - Verify `RevitInstallRoot` for 2024 resolves correctly in build script

10. **Update dependencies for .NET 8.0 / .NET 10** (if needed):
    - Review NuGet packages for 2025+ .NET 8.0 / 2027 .NET 10 compatibility
    - Add binding redirects to `app.config` if Microsoft.Extensions versions conflict (esp. for 2025-2027)

---

**Phase 4: Verification & Build Testing**

11. **Parallel test builds** (one per .csproj file):
    - Build Debug | Any CPU for each version-specific project
    - Verify output in: `bin\Debug\Revit20XX\net[481|8.0|10.0]\ExcelExporterImporter.dll`
    - Check `.csproj` file syntax in VS Properties dialog (All Frameworks dropdown)

12. **Conditional compilation verification**:
    - In each project, hover over preprocessor symbols in source files to confirm correct symbols are visible (REVIT2024 only in 2024 project, REVIT2025+2026 in 2025/2026 projects, etc.)
    - Verify greyed-out code in VS matches expectations for each version

13. **Run full build script**:
    - Execute: `.\Build-PyRevitHybrid.ps1 -RevitVersions 2024,2025,2026,2027`
    - Outputs should land in `axis/axis.extension/bin/Revit20XX/net8.0-windows/`

---

## Relevant Files

- ExcelExporterImporter.csproj → **Replace with:**
  - `ExcelExporterImporter.base.csproj` (abstracted common properties)
  - `ExcelExporterImporter.paths.csproj` (version-specific intermediate paths)
  - `ExcelExporterImporter.2024.csproj` (new)
  - `ExcelExporterImporter.2025.csproj` (new)  
  - `ExcelExporterImporter.2026.csproj` (new)
  - `ExcelExporterImporter.2027.csproj` (new)

- RevitUtilities.cs — Remove lines 462-493
- StandardsExporter.cs — Remove lines 391-435, 464-471, 499-511
- Build-PyRevitHybrid.ps1 — Update version array and project names
- ExcelExporterImporter.sln — Remove old `.csproj`, add four new ones
- Create `dependencies/2024/` folder — Add Revit 2024 SDK DLLs

---

## Verification

1. **Build Success**: All four version-specific projects compile without errors
2. **Output Structure**: DLLs exist in correct paths (`bin/Revit20XX/net[481|8.0|10.0]/ExcelExporterImporter.dll`)
3. **Conditional Code**: Grep for remaining legacy `#if REVIT201[0-9]` symbols — should find zero matches
4. **IntelliSense**: Switch between projects in VS solution and verify IntelliSense shows/hides version-specific APIs correctly
5. **Script Execution**: `Build-PyRevitHybrid.ps1` produces all four versions without errors
6. **Revision History**: Document API breaking changes by version in code comments or documentation

---

## Decisions

- **Approach**: Use article's abstracted `.csproj` pattern (not single `.csproj` with `-p:RevitVersion=` switch) for cleaner IDE support and IntelliSense
- **Symbol Naming**: Keep `REVIT2024`, `REVIT2025`, etc. (consistency with current codebase) vs. article's `RV2024` convention
- **Cleanup Scope**: Remove pre-2024 code only; keep all 2024-2027 code paths for now (conservative approach per user selection)
- **.NET Framework Split**: Separate `obj/$(RevitYear)` folders to prevent conflicts between .NET Framework 4.8.1 (2024) and .NET Core 8.0/10 (2025-2027) MSBuild artifacts

---

## Further Considerations

1. **Question**: Are there Revit 2024 API breaking changes between ElementId.IntegerValue (2024 still uses it?) and `.Value` (2024+)?
   - **Recommendation**: Add `#if !REVIT2024 ... #else ...` wrapper for ElementId usage to handle both int and long safely. Verify 2024 API docs confirm `.Value` property exists in 2024.

2. **Question**: Is the pyRevit hybrid extension manifest ([**extension.json**](axis/axis.extension/extension.json)) set up to load version-specific assemblies from `bin/Revit20XX/` folders?
   - **Recommendation**: Review extension.json loader path to ensure it references `bin/Revit%RevitVersion%/` or similar dynamic path. If not, may need to create version-specific symlinks or use runtime version detection.

3. **Question**: Are all dependencies (EPPlus, log4net, Microsoft.Extensions, Ookii.Dialogs) compatible with .NET Framework 4.8.1, .NET 8.0, and .NET 10?
   - **Recommendation**: Audit ExcelExporterImporter.csproj NuGet versions and app.config binding redirects. May require version-specific PackageReference conditional blocks via `Condition="'$(TargetFramework)'=='net481'"` etc. if major breaking changes exist.

---

**Ready to proceed with implementation?** This plan can be executed as written, with Steps 1-4, 5-7, 8-10, and 11-13 forming four independently verifiable phases.
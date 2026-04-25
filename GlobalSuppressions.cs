// This file contains assembly-level suppression attributes for code-analysis
// rules that cannot be fixed without breaking MAUI platform requirements or
// the project naming convention established by the repository.

using System.Diagnostics.CodeAnalysis;

// ── Namespace / identifier naming ────────────────────────────────────────────
// The root namespace is "kal_sync" because the project is named "kal-sync".
// Hyphens are not valid in C# identifiers so MSBuild converts them to underscores.
// Renaming would break every existing file — suppressed project-wide.
[assembly: SuppressMessage(
    "Naming",
    "CA1707:Remove the underscores from member name",
    Justification = "Project namespace kal_sync derives from project name kal-sync",
    Scope = "namespaceanddescendants",
    Target = "~N:kal_sync")]

// ── MAUI platform types ───────────────────────────────────────────────────────
// AppDelegate is the required entry-point class name on iOS / macOS.
// Renaming it would break the platform-specific MAUI bootstrapping.
[assembly: SuppressMessage(
    "Naming",
    "CA1711:Rename type name AppDelegate so that it does not end in 'Delegate'",
    Justification = "AppDelegate is the MAUI-required iOS/macOS entry-point class name",
    Scope = "type",
    Target = "~T:kal_sync.AppDelegate")]

// ── Default value initialisation ──────────────────────────────────────────────
// The 'count' field in the MAUI template's MainPage is initialised to 0.
// This is template-generated code; the warning is harmless.
[assembly: SuppressMessage(
    "Style",
    "CA1805:Do not initialize unnecessarily",
    Justification = "Template-generated code in MainPage.xaml.cs",
    Scope = "member",
    Target = "~F:kal_sync.MainPage.count")]

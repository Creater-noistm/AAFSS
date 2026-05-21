#!/usr/bin/env python3
"""Generate correct project.assets.json for AAFSS projects."""
import json, os, glob, sys

PACKAGES_DIR = r"C:\Users\noist\.nuget\packages"
SDK_DIR = r"C:\Program Files\dotnet\sdk\9.0.314"
ROOT = r"D:\2026\试点10\AAFSS"

# All package references extracted from csproj files
# Format: project_name -> [(package_id, version), ...]
PROJECT_PACKAGES = {
    "AAFSS.Infrastructure": [
        ("Microsoft.Extensions.DependencyInjection", "8.0.0"),
        ("Microsoft.Extensions.Configuration.Json", "8.0.0"),
        ("Microsoft.EntityFrameworkCore.Sqlite", "8.0.0"),
        ("Microsoft.EntityFrameworkCore.Design", "8.0.0"),
        ("PureHDF", "2.1.0"),
        ("pythonnet", "3.0.3"),
        ("System.Composition", "8.0.0"),
        ("Serilog", "4.0.1"),
        ("Serilog.Sinks.File", "5.0.0"),
        ("Serilog.Extensions.Hosting", "8.0.0"),
        ("CsvHelper", "33.0.0"),
        ("ExcelDataReader", "3.7.0"),
        ("DocX", "3.0.0"),
        ("DocumentFormat.OpenXml", "3.0.0"),
        ("MediatR", "12.2.0"),
        ("ScottPlot", "5.0.47"),
    ],
    "AAFSS.App": [
        ("Microsoft.Extensions.DependencyInjection", "8.0.0"),
        ("Microsoft.Extensions.Configuration.Json", "8.0.0"),
        ("CommunityToolkit.Mvvm", "8.2.2"),
        ("Dirkster.AvalonDock", "4.72.0"),
        ("Fluent.Ribbon", "10.0.0"),
        ("ScottPlot.WPF", "5.0.39"),
        ("HelixToolkit.Wpf", "2.25.0"),
        ("MediatR", "12.2.0"),
        ("Serilog", "4.0.1"),
        ("Serilog.Sinks.File", "5.0.0"),
        ("Serilog.Extensions.Hosting", "8.0.0"),
        ("System.Composition", "8.0.0"),
    ],
    "AAFSS.Core.Tests": [
        ("xunit", "2.8.0"),
        ("xunit.runner.visualstudio", "2.8.0"),
        ("Moq", "4.20.0"),
        ("FluentAssertions", "6.12.0"),
        ("Microsoft.NET.Test.Sdk", "17.9.0"),
    ],
    "AAFSS.Infrastructure.Tests": [
        ("xunit", "2.8.0"),
        ("xunit.runner.visualstudio", "2.8.0"),
        ("Moq", "4.20.0"),
        ("FluentAssertions", "6.12.0"),
        ("Microsoft.NET.Test.Sdk", "17.9.0"),
    ],
}

# Project reference definitions
# Format: project_name -> [(ref_name, relative_path, absolute_path), ...]
PROJECT_REFS = {
    "AAFSS.Infrastructure": [
        ("AAFSS.Core", r"../AAFSS.Core/AAFSS.Core.csproj",
         ROOT + r"\src\AAFSS.Core\AAFSS.Core.csproj"),
        ("AAFSS.PluginContracts", r"../AAFSS.PluginContracts/AAFSS.PluginContracts.csproj",
         ROOT + r"\src\AAFSS.PluginContracts\AAFSS.PluginContracts.csproj"),
    ],
    "AAFSS.App": [
        ("AAFSS.Core", r"../AAFSS.Core/AAFSS.Core.csproj",
         ROOT + r"\src\AAFSS.Core\AAFSS.Core.csproj"),
        ("AAFSS.Infrastructure", r"../AAFSS.Infrastructure/AAFSS.Infrastructure.csproj",
         ROOT + r"\src\AAFSS.Infrastructure\AAFSS.Infrastructure.csproj"),
        ("AAFSS.PluginContracts", r"../AAFSS.PluginContracts/AAFSS.PluginContracts.csproj",
         ROOT + r"\src\AAFSS.PluginContracts\AAFSS.PluginContracts.csproj"),
    ],
    "AAFSS.Core.Tests": [
        ("AAFSS.Core", r"../../src/AAFSS.Core/AAFSS.Core.csproj",
         ROOT + r"\src\AAFSS.Core\AAFSS.Core.csproj"),
        ("AAFSS.Infrastructure", r"../../src/AAFSS.Infrastructure/AAFSS.Infrastructure.csproj",
         ROOT + r"\src\AAFSS.Infrastructure\AAFSS.Infrastructure.csproj"),
    ],
    "AAFSS.Infrastructure.Tests": [
        ("AAFSS.Core", r"../../src/AAFSS.Core/AAFSS.Core.csproj",
         ROOT + r"\src\AAFSS.Core\AAFSS.Core.csproj"),
        ("AAFSS.Infrastructure", r"../../src/AAFSS.Infrastructure/AAFSS.Infrastructure.csproj",
         ROOT + r"\src\AAFSS.Infrastructure\AAFSS.Infrastructure.csproj"),
    ],
}

# Project paths
PROJECT_PATHS = {
    "AAFSS.Infrastructure": ROOT + r"\src\AAFSS.Infrastructure",
    "AAFSS.App": ROOT + r"\src\AAFSS.App",
    "AAFSS.Core.Tests": ROOT + r"\tests\AAFSS.Core.Tests",
    "AAFSS.Infrastructure.Tests": ROOT + r"\tests\AAFSS.Infrastructure.Tests",
}


def get_package_dlls(pkg_dir):
    """Find DLLs in the package's lib directory."""
    lib_dir = os.path.join(pkg_dir, "lib")
    if not os.path.isdir(lib_dir):
        return {}
    
    tfm_order = ["net8.0", "net8.0-windows", "net8.0-windows7.0",
                 "netstandard2.0", "netstandard2.1", "net6.0", "net6.0-windows",
                 "net462", "net461", "net48", "net47", "net472", "net481"]
    
    result = {}
    for tfm in tfm_order:
        tfm_dir = os.path.join(lib_dir, tfm)
        if not os.path.isdir(tfm_dir):
            continue
        found = False
        for f in os.listdir(tfm_dir):
            if f.endswith(".dll"):
                key = f"lib/{tfm}/{f}"
                result[key] = {}
                found = True
        if found:
            break
    return result


def generate_assets(proj_name):
    """Generate project.assets.json for a project."""
    proj_dir = PROJECT_PATHS[proj_name]
    csproj_path = os.path.join(proj_dir, f"{proj_name}.csproj")
    obj_dir = os.path.join(proj_dir, "obj")
    
    # Resolve packages
    targets = {}
    libraries = {}
    dep_list = []
    
    for pkg_name, pkg_version in PROJECT_PACKAGES.get(proj_name, []):
        pkg_key = f"{pkg_name}/{pkg_version}"
        pkg_dir = os.path.join(PACKAGES_DIR, pkg_name.lower(), pkg_version)
        
        if not os.path.isdir(pkg_dir):
            print(f"  SKIP: {pkg_key} (not in cache)")
            continue
        
        dlls = get_package_dlls(pkg_dir)
        tgt_entry = {"type": "package"}
        if dlls:
            tgt_entry["compile"] = dlls
            tgt_entry["runtime"] = dlls
        targets[pkg_key] = tgt_entry
        libraries[pkg_key] = {"type": "package"}
        dep_list.append(f"{pkg_name} >= {pkg_version}")
    
    # Resolve project references
    proj_ref_dict = {}
    for ref_name, rel_path, abs_path in PROJECT_REFS.get(proj_name, []):
        pr_key = f"{ref_name}/1.0.0"
        targets[pr_key] = {
            "type": "project",
            "framework": ".NETCoreApp,Version=v8.0",
            "compile": {f"bin/placeholder/{ref_name}.dll": {}},
            "runtime": {f"bin/placeholder/{ref_name}.dll": {}},
        }
        libraries[pr_key] = {
            "type": "project",
            "path": rel_path,
            "msbuildProject": rel_path,
        }
        proj_ref_dict[abs_path] = {"projectPath": abs_path}
    
    out_path = proj_dir.replace("\\", "\\\\") + "\\\\obj\\\\"
    csproj_unique = csproj_path.replace("\\", "\\\\")
    
    assets = {
        "version": 3,
        "targets": {
            "net8.0-windows7.0": targets
        },
        "libraries": libraries,
        "projectFileDependencyGroups": {
            "net8.0-windows7.0": dep_list
        },
        "packageFolders": {
            PACKAGES_DIR: {}
        },
        "project": {
            "version": "1.0.0",
            "restore": {
                "projectUniqueName": csproj_unique,
                "projectName": proj_name,
                "projectPath": csproj_unique,
                "packagesPath": PACKAGES_DIR,
                "outputPath": out_path,
                "projectStyle": "PackageReference",
                "configFilePaths": [
                    r"C:\Users\noist\AppData\Roaming\NuGet\NuGet.Config"
                ],
                "originalTargetFrameworks": ["net8.0-windows"],
                "sources": {
                    "https://api.nuget.org/v3/index.json": {}
                },
                "frameworks": {
                    "net8.0-windows7.0": {
                        "targetAlias": "net8.0-windows",
                        "projectReferences": proj_ref_dict,
                    }
                },
                "warningProperties": {"warnAsError": ["NU1605"]},
                "restoreAuditProperties": {
                    "enableAudit": "true",
                    "auditLevel": "low",
                    "auditMode": "direct",
                },
                "SdkAnalysisLevel": "9.0.300",
            },
            "frameworks": {
                "net8.0-windows7.0": {
                    "targetAlias": "net8.0-windows",
                    "imports": ["net461","net462","net47","net471","net472","net48","net481"],
                    "assetTargetFallback": True,
                    "warn": True,
                    "frameworkReferences": {
                        "Microsoft.NETCore.App": {"privateAssets": "all"}
                    },
                    "runtimeIdentifierGraphPath": SDK_DIR + "\\PortableRuntimeIdentifierGraph.json",
                }
            },
        },
    }
    
    os.makedirs(obj_dir, exist_ok=True)
    output_file = os.path.join(obj_dir, "project.assets.json")
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(assets, f, indent=2, ensure_ascii=False)
    print(f"  OK: {len(json.dumps(assets))} chars -> {output_file}")


if __name__ == "__main__":
    for name in ["AAFSS.Infrastructure", "AAFSS.App", 
                  "AAFSS.Core.Tests", "AAFSS.Infrastructure.Tests"]:
        print(f"Generating: {name}")
        generate_assets(name)
    print("\nAll done!")

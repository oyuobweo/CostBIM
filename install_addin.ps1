$addinPath = "d:\CostBim\CostBIM.addin"

# Revit versions and their target frameworks (Optimized for Revit 2026 Only)
$revitVersions = @{
    # "2019" = "net48"
    # "2022" = "net48"
    # "2024" = "net48"
    # "2025" = "net8.0-windows"
    "2026" = "net8.0-windows"
}

foreach ($v in $revitVersions.Keys) {
    $destDir = "C:\Users\LYH\AppData\Roaming\Autodesk\Revit\Addins\$v"
    if (Test-Path $destDir) {
        $framework = $revitVersions[$v]
        
        $loaderDllPath = "d:\CostBim\CostBIM.Loader\bin\x64\Debug\$framework\CostBIM.Loader.dll"
        $addinDllPath = "d:\CostBim\bin\x64\Debug\$framework\CostBIM.dll"
        $addinPdbPath = "d:\CostBim\bin\x64\Debug\$framework\CostBIM.pdb"
        
        if (Test-Path $loaderDllPath) {
            # 1) Copy Loader DLL (only if unlocked/not running, or quietly catch)
            try {
                Copy-Item -Path $loaderDllPath -Destination $destDir -Force -ErrorAction SilentlyContinue
            } catch {
                # Loader might be locked, but since it's a static bootstrapper, we don't need to update it every time!
            }

            # 2) Copy main Addin DLL and PDB (NEVER LOCKED because Loader uses memory bytes loading!)
            if (Test-Path $addinDllPath) {
                Copy-Item -Path $addinDllPath -Destination $destDir -Force
                if (Test-Path $addinPdbPath) {
                    Copy-Item -Path $addinPdbPath -Destination $destDir -Force
                }
                Write-Output "⚡ Revit ${v} (${framework}): 최신 Addin DLL 핫로드 배포 완료! (잠금 없음)"
            } else {
                Write-Output "❌ Addin DLL을 찾을 수 없습니다: $addinDllPath"
            }

            # 3) Copy manifest
            Copy-Item -Path $addinPath -Destination $destDir -Force
            Write-Output "⚡ Revit ${v} (${framework}): .addin 매니페스트 복사 완료!"
        } else {
            Write-Output "❌ Loader DLL을 찾을 수 없습니다: $loaderDllPath"
        }

        # 4) Clean up old legacy files if they exist to prevent duplicate load errors
        $legacyFiles = @("RevitQTO.Addin.addin", "RevitQTO.Addin.dll", "RevitQTO.Addin.pdb", "ParameterExtractor.addin", "ParameterExtractor.dll", "ParameterExtractor.pdb")
        foreach ($file in $legacyFiles) {
            $legacyPath = Join-Path $destDir $file
            if (Test-Path $legacyPath) {
                Remove-Item -Path $legacyPath -Force -ErrorAction SilentlyContinue
                Write-Output "🧹 Revit ${v}: 구버전 유산 (${file}) 자동 소거 완료!"
            }
        }
    }
}

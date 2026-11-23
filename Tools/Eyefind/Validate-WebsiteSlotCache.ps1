$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot '../../lumen-sante/Gameplay/WebsiteSlotCache.cs'
$source = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $sourcePath))
$harness = @'
namespace lumen_sante.Gameplay
{
    public static class WebsiteSlotCacheValidation
    {
        public static void Run()
        {
            var cache = new WebsiteSlotCache();
            object[] original = { "paciente", 7, "" };
            if (cache.IsCurrent(200, original)) throw new System.Exception("Novo slot foi considerado enviado.");
            cache.Commit(200, original);
            if (!cache.IsCurrent(200, new object[] { "paciente", 7, "" })) throw new System.Exception("Valores iguais não foram reconhecidos.");
            if (cache.IsCurrent(201, original)) throw new System.Exception("Slots diferentes foram confundidos.");
            if (cache.IsCurrent(200, new object[] { "paciente", 8, "" })) throw new System.Exception("Alteração não foi detectada.");
            if (cache.IsCurrent(200, new object[] { "paciente", 7 })) throw new System.Exception("Mudança de tamanho não foi detectada.");
            original[1] = 8;
            if (!cache.IsCurrent(200, new object[] { "paciente", 7, "" })) throw new System.Exception("O cache reteve um array mutável.");
            cache.Clear();
            if (cache.IsCurrent(200, new object[] { "paciente", 7, "" })) throw new System.Exception("Limpeza não invalidou o slot.");
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $harness)
[lumen_sante.Gameplay.WebsiteSlotCacheValidation]::Run()
'Cache de slots validado.'

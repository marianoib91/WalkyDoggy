# Crea el primer administrador de WalkyDoggy (los siguientes se dan de alta desde el panel de administracion).
# Uso:  .\crear-administrador.ps1 -Email admin@walkydoggy.com [-Password ...] [-Servidor '(local)\SQLEXPRESS'] [-Base WalkyDoggy]
# Si no se indica la contrasena, se genera una al azar y se guarda en admin-inicial.txt (ignorado por git).
# Despues de entrar conviene cambiarla desde el menu del usuario > Cambiar contrasena.
param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$Password,
    [string]$Servidor = '(local)\SQLEXPRESS',
    [string]$Base = 'WalkyDoggy'
)

$ErrorActionPreference = 'Stop'

if (-not $Password) {
    $Password = 'Ad' + [guid]::NewGuid().ToString('N').Substring(0, 12)
    $archivo = Join-Path $PSScriptRoot 'admin-inicial.txt'
    Set-Content -Path $archivo -Value ("Email: $Email" + [Environment]::NewLine + "Password: $Password") -Encoding UTF8
    Write-Host "Contrasena generada y guardada en $archivo"
}

if ($Password.Length -lt 6 -or $Password.Length -gt 50) { throw 'La contrasena debe tener entre 6 y 50 caracteres.' }
if ($Email -notmatch '^[^@\s]+@[^@\s]+\.[^@\s]+$') { throw 'El mail no es valido.' }

# Mismo algoritmo que ServicioEncriptacion: sal de 16 bytes en base 64 y SHA-256 de (sal + contrasena)
$bytesSal = New-Object byte[] 16
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytesSal)
$sal = [Convert]::ToBase64String($bytesSal)
$sha = [Security.Cryptography.SHA256]::Create()
$hash = [Convert]::ToBase64String($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($sal + $Password)))

$emailSql = $Email.Replace("'", "''")
$sql = @"
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM Users WHERE Email = '$emailSql')
BEGIN
    SELECT 'Ya existe un usuario con ese mail: no se creo nada.' AS Resultado;
END
ELSE
BEGIN
    BEGIN TRAN;
    INSERT INTO Users (Email, HashedPassword, Salt, IsLocked, CreatedDate) VALUES ('$emailSql', '$hash', '$sal', 0, GETDATE());
    INSERT INTO UserRoles (UserId, RoleId) VALUES (SCOPE_IDENTITY(), 1);
    COMMIT;
    SELECT 'Administrador creado.' AS Resultado;
END
"@

$temporal = [IO.Path]::GetTempFileName()
try {
    Set-Content -Path $temporal -Value $sql -Encoding UTF8
    sqlcmd -S $Servidor -d $Base -E -b -h -1 -i $temporal
}
finally {
    Remove-Item $temporal -Force -ErrorAction SilentlyContinue
}

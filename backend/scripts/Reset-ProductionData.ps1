<#
.SYNOPSIS
    Gatilho manual para limpar a massa de dados de teste/validação do Renovo
    Centro Automotivo antes de liberar o sistema para uso real.

.DESCRIPTION
    Isto NUNCA roda sozinho — é um script que você executa manualmente, uma
    única vez, depois que o período de validação/testes terminar. Ele:
      1. Sempre mostra antes quantos registros seriam afetados (preview).
      2. Só apaga alguma coisa se você digitar a frase de confirmação exata.
      3. Remove somente dados transacionais de teste e as 10 contas de
         demonstração semeadas pelo sistema — nunca usuários criados
         manualmente durante a validação.

.PARAMETER ConnectionString
    Connection string do Postgres de PRODUÇÃO (a mesma usada em
    ConnectionStrings__DefaultConnection no Railway). Nunca deixe isso salvo
    em texto puro em algum arquivo do repositório — cole na hora ou passe via
    variável de ambiente.

.PARAMETER DryRun
    Só mostra as contagens (preview-production-data.sql) e sai, sem apagar
    nada e sem pedir confirmação.

.EXAMPLE
    ./Reset-ProductionData.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..." -DryRun

.EXAMPLE
    ./Reset-ProductionData.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..."
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$previewSql = Join-Path $scriptDir "preview-production-data.sql"
$resetSql = Join-Path $scriptDir "reset-production-data.sql"

$psql = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psql) {
    Write-Host "psql não foi encontrado no PATH." -ForegroundColor Yellow
    Write-Host "Instale o cliente do PostgreSQL, ou cole o conteúdo destes arquivos manualmente"
    Write-Host "no console de query do Railway (Postgres > Query):"
    Write-Host "  Preview (só leitura): $previewSql"
    Write-Host "  Limpeza real:         $resetSql"
    exit 1
}

Write-Host "== Prévia: registros que seriam afetados ==" -ForegroundColor Cyan
& psql $ConnectionString -f $previewSql
if ($LASTEXITCODE -ne 0) {
    Write-Host "Falha ao consultar o banco. Verifique a connection string." -ForegroundColor Red
    exit 1
}

if ($DryRun) {
    Write-Host ""
    Write-Host "Modo -DryRun: nada foi apagado." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "ATENÇÃO: isto vai apagar clientes, veículos, ordens de serviço, checklists," -ForegroundColor Yellow
Write-Host "ordens de compra, logs de WhatsApp e as 10 contas de demonstração (incluindo" -ForegroundColor Yellow
Write-Host "'admin') do banco de PRODUÇÃO. Contas criadas manualmente não são afetadas." -ForegroundColor Yellow
Write-Host "Confirme antes que já existe uma conta administradora REAL cadastrada —" -ForegroundColor Yellow
Write-Host "depois deste script você perde o acesso ao login 'admin'." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Digite exatamente `"LIMPAR DADOS DE TESTE`" para confirmar"

if ($confirmation -ne "LIMPAR DADOS DE TESTE") {
    Write-Host "Confirmação não corresponde. Nada foi apagado." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "== Executando a limpeza ==" -ForegroundColor Cyan
& psql $ConnectionString -f $resetSql
if ($LASTEXITCODE -ne 0) {
    Write-Host "A limpeza falhou no meio do caminho — a transação foi revertida (nada ficou pela metade)." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Concluído. Próximos passos recomendados:" -ForegroundColor Green
Write-Host "  1. Crie/confirme o acesso da conta administradora real (se ainda não existe uma além da 'admin' apagada)."
Write-Host "  2. Revise Configurações da oficina (nome, telefone, endereço, logo)."
Write-Host "  3. Rotacione Jwt:Key, WhatsApp:WebhookToken e a chave da Evolution API se algum colaborador externo teve acesso a elas durante a validação."

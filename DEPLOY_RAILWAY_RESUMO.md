# Deploy no Railway - Resumo Executivo

## ✅ O que foi configurado

### 1. Arquivos Criados/Modificados

#### **GUIA_DEPLOY_RAILWAY.md** (NOVO)
- Guia completo passo a passo para deploy no Railway
- Inclui configuração de variáveis de ambiente
- Instruções para deploy do frontend (Netlify/Vercel)
- Troubleshooting e monitoramento

#### **backend/RenovoWorkshop.Api/Program.cs** (MODIFICADO)
- ✅ Adicionado suporte a CORS dinâmico baseado em ambiente
- ✅ Política `AllowProduction` que lê origens do `appsettings.Production.json`
- ✅ JWT agora valida Issuer e Audience em produção
- ✅ Usa `app.Environment.IsDevelopment()` para escolher política CORS

#### **backend/RenovoWorkshop.Api/appsettings.Production.json** (NOVO)
- Configurações específicas para produção
- Inclui JWT com Issuer/Audience
- CORS com origens configuráveis
- **Importante**: Alterar `Jwt:Key` em produção!

#### **backend/.env.example** (NOVO)
- Template de variáveis de ambiente
- Documenta todas as variáveis necessárias
- Inclui exemplos de valores

#### **backend/railway.json** (JÁ EXISTIA)
- Configurado para build com `dotnet publish`
- Start command correto: `cd publish && dotnet RenovoWorkshop.Api.dll`

### 2. Banco de Dados

**Recomendação**: Usar PostgreSQL no Railway
- Crie um PostgreSQL no Railway (New → Database → PostgreSQL)
- O Railway fornece automaticamente a variável `DATABASE_URL`
- **Atenção**: O código atual usa SQLite, será necessário adaptar para PostgreSQL

**Alternativa**: SQLite (não recomendado para produção)
- Funciona sem configuração adicional
- Dados são perdidos quando o serviço reinicia

## 🚀 Passos Rápidos para Deploy

### 1. Preparar Repositório
```bash
# Faça commit das alterações
git add .
git commit -m "feat: configure Railway deployment"
git push origin main
```

### 2. Criar Projeto no Railway
1. Acesse [railway.app](https://railway.app)
2. Login com GitHub
3. New Project → Deploy from GitHub repo
4. Selecione: `wellagoravai/renovo-centro-automotivo-backend`

### 3. Configurar Variáveis de Ambiente
No Railway, vá em **Variables** e adicione:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
Jwt__Key=<GERE_UMA_CHAVE_SEGURA_AQUI>
Jwt__Issuer=RenovoWorkshop.Api
Jwt__Audience=RenovoWorkshop.Client
Jwt__ExpirationInMinutes=60
Cors__AllowedOrigins=https://seu-frontend.netlify.app
```

**Como gerar JWT Key segura (PowerShell)**:
```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

### 4. (Opcional) Adicionar PostgreSQL
1. No Railway: New → Database → PostgreSQL
2. Anote a `DATABASE_URL` fornecida
3. Adapte o código para usar PostgreSQL (ver guia completo)

### 5. Deploy do Frontend (Vercel)
O frontend React será hospedado no **Vercel**:

1. Acesse [vercel.com](https://vercel.com)
2. Faça login com GitHub
3. Clique em **"Add New Project"**
4. Importe o repositório: `wellagoravai/renovo-centro-automotivo-backend`
5. Configure:
   - **Framework Preset**: React
   - **Build Command**: `npm run build`
   - **Output Directory**: `build`
   - **Root Directory**: `./` (raiz do repositório)
6. Adicione variável de ambiente:
   ```env
   REACT_APP_API_URL=https://seu-backend.railway.app
   ```
7. Clique em **"Deploy"**

**Importante**: O Vercel vai detectar automaticamente o `package.json` na raiz e fazer o build do React.

### 6. Atualizar CORS
Após deploy do frontend no Vercel, atualize `Cors__AllowedOrigins` no Railway com a URL do frontend (ex: `https://seu-projeto.vercel.app`).

## 📊 Custos

### Railway Free Tier
- **$5 de crédito/mês grátis para sempre**
- Suficiente para ~500 horas de execução
- Perfeito para projetos pequenos

### Quando começar a pagar
- Após gastar os $5 de crédito
- Custo: ~$0.50/hora
- Para API pequena: ~$5-10/mês

## ⚠️ Avisos Importantes

### 1. Banco de Dados
- **SQLite**: Dados perdidos ao reiniciar (não use em produção)
- **PostgreSQL**: Recomendado, mas requer adaptação do código

### 2. JWT Key
- **NUNCA** use a chave de desenvolvimento em produção
- Gere uma chave segura e única
- Guarde-a em local seguro (variável de ambiente)

### 3. CORS
- Adicione apenas domínios confiáveis
- Não use `*` em produção
- Inclua `https://` nas URLs

### 4. Domínio
- Railway fornece domínio gratuito: `https://seu-app.railway.app`
- Para domínio customizado: Settings → Domains

## 🔍 Monitoramento

O Railway oferece:
- **Logs em tempo real**: Deployments → View Logs
- **Métricas**: CPU, memória, requests
- **Alertas**: Configure em Settings

## 📝 Checklist Final

- [ ] Código commitado e pushed para GitHub
- [ ] Projeto criado no Railway
- [ ] Variáveis de ambiente configuradas
- [ ] JWT Key segura gerada
- [ ] (Opcional) PostgreSQL criado
- [ ] Backend deployado e funcionando
- [ ] Frontend deployado (Netlify/Vercel)
- [ ] CORS atualizado com URL do frontend
- [ ] Testado login e endpoints principais
- [ ] Monitoramento configurado

## 🆘 Problemas Comuns

### Build Falha
- Verifique se `railway.json` está na raiz
- Confira se o .NET SDK está disponível

### App não inicia
- Verifique logs no Railway
- Confira se `ASPNETCORE_URLS` está configurado
- Verifique se a porta é `$PORT`

### Erro de CORS
- Adicione a URL do frontend em `Cors__AllowedOrigins`
- Use `https://` sem barra no final

### Banco de dados não conecta
- Se usar PostgreSQL, confira `DATABASE_URL`
- Verifique se o connection string está correto

## 📚 Documentação Adicional

- [GUIA_DEPLOY_RAILWAY.md](GUIA_DEPLOY_RAILWAY.md) - Guia completo
- [Railway Docs](https://docs.railway.app)
- [Railway .NET Guide](https://docs.railway.app/guides/dotnet)

## 🎯 Próximos Passos

1. Fazer commit e push das alterações
2. Criar projeto no Railway
3. Configurar variáveis de ambiente
4. Fazer deploy do backend
5. Fazer deploy do frontend
6. Testar aplicação completa
7. Configurar domínio customizado (opcional)
8. Configurar CI/CD (automático)

---

**Status**: ✅ Pronto para deploy!
**Tempo estimado**: 15-30 minutos
**Custo**: Grátis (dentro do free tier)
# Guia de Deploy - Railway.app

Este guia explica como fazer deploy da aplicação Renovo Centro Automotivo no Railway.app de forma gratuita.

## 📋 Pré-requisitos

- Conta no [Railway.app](https://railway.app) (pode usar login com GitHub)
- Conta no [GitHub](https://github.com) (o repositório já está em: `https://github.com/wellagoravai/renovo-centro-automotivo-backend.git`)

## 🚀 Passo a Passo para Deploy

### 1. Preparar o Repositório

O projeto já está configurado para deploy no Railway com o arquivo `railway.json`.

**Importante**: O Railway vai fazer deploy apenas da pasta `backend/`. Precisamos garantir que o arquivo `railway.json` está na raiz do repositório (já está ✅).

### 2. Criar Projeto no Railway

1. Acesse [railway.app](https://railway.app) e faça login
2. Clique em **"New Project"**
3. Selecione **"Deploy from GitHub repo"**
4. Escolha o repositório: `wellagoravai/renovo-centro-automotivo-backend`
5. Clique em **"Add variables"** e configure as variáveis de ambiente (veja abaixo)
6. Clique em **"Deploy"**

### 3. Configurar Variáveis de Ambiente

No Railway, vá em **Variables** e adicione:

#### Variáveis Obrigatórias:

```env
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Banco de Dados (Railway vai fornecer automaticamente se criar um PostgreSQL)
# Se usar SQLite (não recomendado para produção):
# ConnectionStrings__DefaultConnection=Data Source=renovo.db

# JWT (gere uma chave segura!)
Jwt__Key=SUA_CHAVE_SECRETA_AQUI_GERE_UMA_FORTE
Jwt__Issuer=RenovoWorkshop.Api
Jwt__Audience=RenovoWorkshop.Client
Jwt__ExpirationInMinutes=60

# CORS (adicione a URL do frontend após deploy)
Cors__AllowedOrigins=https://seu-frontend.netlify.app,https://seu-frontend.vercel.app
```

#### Como gerar JWT Key segura:
```bash
# No PowerShell:
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

### 4. Configurar Banco de Dados (Recomendado)

#### Opção A: PostgreSQL (Recomendado)
1. No Railway, clique em **"New"** → **"Database"** → **"PostgreSQL"**
2. O Railway vai criar automaticamente a variável `DATABASE_URL`
3. No código, você precisará adaptar para usar PostgreSQL ao invés de SQL Server

#### Opção B: SQLite (Mais simples, mas não recomendado para produção)
- Não precisa configurar nada, o Railway vai usar o arquivo local
- **Atenção**: Os dados serão perdidos se o serviço for reiniciado

### 5. Configurar Domínio Customizado (Opcional)

1. No Railway, vá em **Settings** → **Domains**
2. Clique em **"Generate Domain"** para obter um domínio gratuito do Railway
3. Ou configure seu próprio domínio (ex: `api.renovo.com`)

### 6. Deploy do Frontend (Vercel)

O frontend React será hospedado no **Vercel** (recomendado para projetos React):

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

### 7. Atualizar CORS

Após fazer deploy do frontend no Vercel, volte no Railway e atualize a variável `Cors__AllowedOrigins` com a URL do frontend (ex: `https://seu-projeto.vercel.app`).

## 📊 Monitoramento

O Railway oferece:
- **Logs em tempo real**: Veja os logs da aplicação
- **Métricas**: CPU, memória, requests
- **Alertas**: Configure alertas para quando o serviço cair

## 💰 Custos

### Free Tier do Railway:
- **$5 de crédito gratuito por mês** (para sempre!)
- Suficiente para ~500 horas de execução
- Perfeito para projetos pequenos

### Quando começar a pagar:
- Após gastar os $5 de crédito
- Custo: ~$0.50/hora de uso
- Para uma API pequena: ~$5-10/mês

## 🔧 Troubleshooting

### Erro: "Failed to build"
- Verifique se o arquivo `railway.json` está na raiz
- Confira se o comando de build está correto: `dotnet publish -c Release -o ./publish`

### Erro: "Application failed to start"
- Verifique os logs no Railway
- Confira se a porta está correta: `$PORT`
- Verifique se todas as variáveis de ambiente estão configuradas

### Erro de conexão com banco de dados
- Se usar PostgreSQL, confira se a variável `DATABASE_URL` está configurada
- Verifique se o connection string está correto

### CORS Error no frontend
- Adicione a URL do frontend em `Cors__AllowedOrigins`
- Não esqueça do `https://` e sem barra no final

## 📝 Checklist de Deploy

- [ ] Conta Railway criada
- [ ] Repositório GitHub conectado
- [ ] Variáveis de ambiente configuradas
- [ ] Banco de dados configurado (PostgreSQL recomendado)
- [ ] Backend deployado e funcionando
- [ ] Frontend deployado (Netlify/Vercel)
- [ ] CORS configurado com URL do frontend
- [ ] Testado endpoints da API
- [ ] Testado fluxo completo do site

## 🎯 Próximos Passos

1. Fazer deploy do backend no Railway
2. Fazer deploy do frontend no Netlify/Vercel
3. Configurar domínio customizado (opcional)
4. Configurar CI/CD (automático a cada push no GitHub)
5. Adicionar monitoramento e alertas

## 📚 Recursos Úteis

- [Railway Documentation](https://docs.railway.app)
- [Railway .NET Guide](https://docs.railway.app/guides/dotnet)
- [Netlify Documentation](https://docs.netlify.com)
- [Vercel Documentation](https://vercel.com/docs)

## 🆘 Suporte

Se tiver problemas:
1. Verifique os logs no Railway (aba "Deployments" → clique no deploy → "View Logs")
2. Consulte a documentação do Railway
3. Abra uma issue no GitHub do projeto

---

*Última atualização: 21/07/2026 - Trigger de deploy Railway*

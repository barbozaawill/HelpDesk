# HelpDesk

Sistema de abertura e atendimento de chamados com controle de SLA, mensageria assíncrona e dashboard de métricas em tempo real.

Construído com **ASP.NET Core 8**, **Blazor WebAssembly**, **PostgreSQL**, **RabbitMQ** e **Redis**, tudo orquestrado com **Docker Compose** e com pipeline de **CI/CD no GitHub Actions**.

---

## Funcionalidades

- **Autenticação com JWT** e três perfis de acesso: Cliente, Atendente e Admin
- **Abertura de chamados** com prioridade (Baixa, Média, Alta, Crítica) e categoria
- **SLA automático:** prazos de primeira resposta e de resolução calculados a partir da prioridade
- **Máquina de estados do chamado,** com as transições validadas no domínio
- **Eventos de integração** publicados no RabbitMQ a cada mudança de estado
- **Worker em background** que consome os eventos e atualiza as métricas no Redis
- **Dashboard** para atendentes e admins com indicadores em tempo real
- **Front-end em Blazor WebAssembly** consumindo a API

---

## Arquitetura

```
                      ┌──────────────┐
   Navegador  ──────► │ HelpDesk.Web │  Blazor WebAssembly (servido por nginx)
                      └──────┬───────┘
                             │ HTTP + JWT
                      ┌──────▼───────┐        ┌────────────┐
                      │ HelpDesk.Api │ ─────► │ PostgreSQL │  fonte da verdade
                      └──────┬───────┘        └─────▲──────┘
             publica eventos │                      │
                      ┌──────▼───────┐              │
                      │   RabbitMQ   │              │ reconciliação a cada 30s
                      └──────┬───────┘              │
              consome eventos│                      │
                      ┌──────▼──────────┐           │
                      │ HelpDesk.Worker │ ──────────┘
                      └──────┬──────────┘
                             │ contadores (HINCRBY)
                      ┌──────▼───────┐
                      │    Redis     │ ◄──── a API lê as métricas do dashboard
                      └──────────────┘
```

### Projetos da solução

| Projeto | Responsabilidade |
|---|---|
| `HelpDesk.Domain` | Entidades, enums, regras de negócio, cálculo de SLA e eventos. Não depende de nenhum outro projeto. |
| `HelpDesk.Infrastructure` | EF Core + PostgreSQL, cache no Redis, publicação no RabbitMQ e hash de senha. |
| `HelpDesk.Api` | Controllers REST, autenticação JWT, middleware de erros e Swagger. |
| `HelpDesk.Worker` | Consome os eventos do RabbitMQ e reconcilia as métricas periodicamente. |
| `HelpDesk.Web` | Interface em Blazor WebAssembly. |
| `HelpDesk.Tests` | Testes unitários (xUnit) do domínio e do serviço de chamados. |

As regras de negócio ficam na entidade `Chamado`, e não nos controllers. Uma transição inválida (por exemplo, fechar um chamado que ainda não foi resolvido) lança uma `RegraDeNegocioException`, que o middleware converte em `400 Bad Request` (e `RecursoNaoEncontradoException` em `404`).

---

## Regras de negócio

### Ciclo de vida do chamado

```
Aberto ──► EmAtendimento ◄──► AguardandoCliente
                │
                ▼
            Resolvido ──► Fechado
                │            │
                └──► Reaberto ◄┘
```

- Só um **Atendente** ou **Admin** pode assumir ou resolver um chamado
- Um chamado só pode ser **fechado** depois de **resolvido**
- Um chamado **fechado** não aceita respostas; é preciso reabrir antes
- Só é possível **reabrir** um chamado resolvido ou fechado

### Política de SLA

| Prioridade | Primeira resposta | Resolução |
|---|---|---|
| Baixa | 8 h | 72 h |
| Média | 4 h | 24 h |
| Alta | 1 h | 8 h |
| Crítica | 15 min | 4 h |

### Métricas: rápidas e corretas

O Worker atualiza contadores no Redis a cada evento, o que deixa o dashboard instantâneo. Como os eventos podem chegar fora de ordem, esses números são **aproximados**. Por isso existe um segundo processo (`ReconciliadorDeMetricas`) que, a cada 30 segundos, recalcula tudo a partir do PostgreSQL e corrige qualquer desvio, incluindo métricas que dependem do horário atual, como SLA estourado.

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| API | ASP.NET Core 8, Swagger/OpenAPI |
| Front-end | Blazor WebAssembly, Bootstrap |
| Banco de dados | PostgreSQL 16 + Entity Framework Core |
| Mensageria | RabbitMQ (exchange do tipo topic) |
| Cache | Redis 7 |
| Autenticação | JWT Bearer |
| Testes | xUnit |
| Infraestrutura | Docker, Docker Compose, nginx |
| CI/CD | GitHub Actions + GitHub Container Registry |

---

## Como rodar

### Com Docker (recomendado)

Requisito: [Docker](https://www.docker.com/) com Docker Compose.

```bash
git clone https://github.com/barbozaawill/HelpDesk.git
cd HelpDesk
docker compose up --build
```

Na primeira subida, a API aplica as migrations e cria os dados iniciais automaticamente.

| Serviço | Endereço |
|---|---|
| Aplicação (Blazor) | http://localhost:5001 |
| API + Swagger | http://localhost:5000/swagger |
| Health check do Worker | http://localhost:5100/health |
| Painel do RabbitMQ | http://localhost:15672 (helpdesk / helpdesk) |

### Usuários de demonstração

| Perfil | E-mail | Senha |
|---|---|---|
| Admin | admin@helpdesk.local | Admin@123 |
| Atendente | ana@helpdesk.local | Atende@123 |
| Cliente | carlos@cliente.local | Cliente@123 |

> As senhas e a chave JWT do `docker-compose.yml` servem apenas para o ambiente local de demonstração.

### Testando a API pelo Swagger

1. Faça login em `POST /api/auth/login` com um dos usuários acima
2. Copie o token retornado
3. Clique em **Authorize** e cole o token (sem escrever `Bearer`)

O arquivo `api-requests.http` tem exemplos prontos de requisições.

---

## Testes

```bash
dotnet test
```

Os testes cobrem o cálculo de SLA, as transições da máquina de estados do chamado e o serviço de chamados (com fakes no lugar do banco e da mensageria).

---

## CI/CD

O workflow `.github/workflows/ci-cd.yml` roda em duas etapas:

1. **Build e testes** em todo push e pull request para a `main`
2. **Build e publicação das imagens Docker** (api, worker e web) no GitHub Container Registry, somente em push para a `main` e somente se os testes passarem

---

## Endpoints principais

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/registrar` | Público |
| POST | `/api/auth/login` | Público |
| GET | `/api/auth/eu` | Autenticado |
| GET | `/api/categorias` | Autenticado |
| GET | `/api/chamados` | Autenticado |
| GET | `/api/chamados/{id}` | Autenticado |
| POST | `/api/chamados` | Autenticado |
| PUT | `/api/chamados/{id}/assumir` | Atendente, Admin |
| PUT | `/api/chamados/{id}/responder` | Autenticado |
| PUT | `/api/chamados/{id}/resolver` | Atendente, Admin |
| PUT | `/api/chamados/{id}/fechar` | Autenticado |
| PUT | `/api/chamados/{id}/reabrir` | Autenticado |
| GET | `/api/dashboard/metricas` | Atendente, Admin |

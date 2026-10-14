<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 2

Oi, eu sou o Karlos Dâmaso e este é o espaço onde compartilho minha jornada de aprendizado durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

Aqui você vai encontrar projetos, exercícios e códigos que estou desenvolvendo durante o desafio.

### Sobre este desafio
Neste desafio o objetivo é criar um encurtador de URLs completo utilizando os conceitos de Spec-Driven Development

#### O que foi implementado
Uma API de encurtamento de links em **.NET 10 (Minimal API)**, com **SQLite via EF Core** e testes com **xUnit**, construída a partir das specs em [`specs/`](specs/):

| Documento | Papel |
|---|---|
| [constitution.md](specs/constitution.md) | Regras que não se negociam: stack, arquitetura, qualidade e convenções |
| [spec.md](specs/spec.md) | O quê e por quê: histórias, requisitos (RF), regras de negócio (RN) e critérios de aceite |
| [plan.md](specs/plan.md) | O como: decisões técnicas (D-01 a D-10), dependências, modelo de dados e contratos |
| [tasks.md](specs/tasks.md) | Tarefas T-01 a T-14, cada uma ligada à sua origem e com critério de conclusão |

- **Encurtar URL** com código automático de 7 caracteres (A-Z e 1-9, gerador criptográfico), gerando outro código se houver colisão (RF-01, RF-02, RN-06)
- **Código personalizado** de 4 a 20 caracteres, único no sistema, garantido também por índice único no banco (RF-03, RN-01, RN-05)
- **Redirecionamento 302** direto ao destino, sem tela intermediária (RF-04)
- **Contador de acessos atômico**: um único `UPDATE ... SET AccessCount = AccessCount + 1`, sem perder contagem sob concorrência (RF-05, RN-04)
- **Expiração por data em UTC**: o link vale até o último instante do dia; depois responde 410 e não conta acesso (RF-06, RN-03)
- **Estatísticas públicas** para quem conhece o código (RF-07)
- **Persistência em SQLite** com migração aplicada na inicialização (RF-08)
- **Validação da URL de destino**: só http/https, até 2048 caracteres e host público, recusando localhost, IPs privados, loopback e link-local (RN-02)
- **Erros padronizados** com ProblemDetails e mensagens em português
- **114 testes**: unitários para todas as regras de negócio e de integração para cada endpoint, incluindo concorrência, expiração e reinício da aplicação

#### Endpoints

| Método | Rota | Respostas |
|---|---|---|
| `POST` | `/api/links` | `201` link criado · `400` dados inválidos · `409` código em uso |
| `GET` | `/{code}` | `302` redireciona · `404` não existe · `410` expirado |
| `GET` | `/api/links/{code}/stats` | `200` total de acessos · `404` não existe |

```http
POST /api/links
Content-Type: application/json

{ "targetUrl": "https://balta.io", "code": "BALTA", "expiresAt": "2026-10-31" }
```

```json
{ "shortUrl": "http://localhost:5059/BALTA", "code": "BALTA", "targetUrl": "https://balta.io", "expiresAt": "2026-10-31" }
```

`code` e `expiresAt` são opcionais. A URL base do link curto pode ser fixada em `ShortLinks:BaseUrl` no `appsettings.json`.

#### Como executar

Requer o [SDK do .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/ShortLinks.Api   # API em http://localhost:5059
dotnet test                               # roda os testes
```

O banco `shortlinks.db` é criado automaticamente na primeira execução.

#### Estrutura

```
specs/                     constituição, spec, plano e tarefas
src/ShortLinks.Api/
  Domain/                  entidade Link
  Application/             LinkService, validadores e gerador de código (regras de negócio)
  Data/                    DbContext, repositório e migrações
  Endpoints/               rotas HTTP, sem regra de negócio
tests/ShortLinks.Tests/
  Unit/                    regras de negócio
  Integration/             API em memória com SQLite temporário
```

Neste processo eu aprendi:
* ✅ A ler as specs em ordem (constituição → spec → plano → tarefas) e a tratar a spec como fonte da verdade
* ✅ A encontrar contradições antes de codar: o plano exigia uma chave de acesso nas estatísticas que a spec não pedia, citava um requisito inexistente (DC-03) e incluía um cache sem origem
* ✅ A rastrear cada tarefa, decisão e teste até o requisito que o originou
* ✅ A transformar trechos vagos da spec ("publicamente acessível", "até o fim da data") em decisões explícitas no plano
* ✅ A garantir regras sob concorrência no banco (UPDATE atômico e índice único) em vez de confiar só na aplicação
* ✅ A testar regras dependentes de tempo com `TimeProvider`, sem depender do relógio real

## Bagde
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/02.png" width="200" />

## Problema
Links longos quebram em mensagens, e-mails e materiais impressos, e não dá para medir quantas pessoas clicaram neles. O encurtador transforma uma URL longa em um link curto, que pode ser compartilhado e cujos acessos podem ser acompanhados.

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
[Incluir link para o repositório central]

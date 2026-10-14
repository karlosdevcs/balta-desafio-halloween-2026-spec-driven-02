# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição
do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- Endpoint só lê a entrada, chama o serviço e traduz o resultado em resposta;
  toda validação de regra de negócio (URL, código, expiração) vive no serviço
  ou em validadores da camada de aplicação, cobertos por teste unitário

## Decisões
- D-01: SQLite como armazenamento, porque o RF-08 exige persistência
  entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e
  reescrito na aplicação, para atender a RN-04 sob acesso concorrente; o
  mesmo UPDATE filtra por link não expirado, para atender a RN-03
- D-03: código do link gerado com o alfabeto definido no RF-02 e na RN-05:
  letras maiúsculas de A a Z e dígitos de 1 a 9 (35 símbolos), usando gerador
  criptográfico (RandomNumberGenerator) para evitar códigos previsíveis
- D-04: unicidade do código garantida por índice único no banco, além da
  checagem no serviço, porque a checagem sozinha tem corrida
- D-05: na geração automática (RN-06), o serviço tenta até 5 códigos; se o
  índice único recusar todos, retorna erro 500 com ProblemDetails. Com 35^7
  combinações, esgotar as tentativas é improvável
- D-06: "publicamente acessível" (RN-02) é verificado sem requisição externa:
  a URL precisa ser absoluta, usar http ou https, ter host e não apontar para
  localhost, endereço de loopback, rede privada (10/8, 172.16/12, 192.168/16,
  fc00::/7), link-local (169.254/16, fe80::/10), endereço não especificado
  (0.0.0.0/8, ::) ou nome de host sem ponto (ex.: intranet).
  Não há resolução de DNS, para manter os testes determinísticos e evitar uso
  da API como scanner de rede
- D-07: a expiração (RF-06) é uma data, sem hora, armazenada como ExpiresOn
  (DateOnly). O link é válido enquanto a data atual em UTC for menor ou igual
  a ExpiresOn, ou seja, até o último instante desse dia em UTC. Uma data
  anterior à data atual em UTC é recusada na criação; a data de hoje é aceita
- D-08: o relógio é abstraído por TimeProvider, para testar a expiração sem
  depender da hora real
- D-09: códigos são comparados exatamente como recebidos, sem normalizar
  maiúsculas; um código fora do formato no acesso responde 404
- D-10: a URL base do shortUrl vem da configuração `ShortLinks:BaseUrl`; se
  não estiver configurada, usa o esquema e o host da requisição

## Dependências
- Microsoft.EntityFrameworkCore.Sqlite: provedor exigido pela D-01
- Microsoft.EntityFrameworkCore.Design: geração das migrações do EF Core
- dotnet-ef (ferramenta local do repositório): comando que gera as migrações
- xunit e xunit.runner.visualstudio: framework de testes da constituição
- Microsoft.NET.Test.Sdk: necessário para executar os testes com dotnet test
- Microsoft.AspNetCore.Mvc.Testing: hospedagem da API em memória nos testes
  de integração exigidos pela constituição

## Modelo de dados
Link
- Id, Code (único, até 20 caracteres), TargetUrl (até 2048 caracteres),
  CreatedAtUtc, ExpiresOn (data, nulo), AccessCount

## Contratos
Todo erro é retornado como ProblemDetails, com mensagem em português.

- POST /api/links
  - entrada: targetUrl, code opcional, expiresAt opcional (data, AAAA-MM-DD)
  - 201 com header Location e corpo: shortUrl, code, targetUrl, expiresAt
  - 400 para URL inválida, não pública ou com mais de 2048 caracteres, código
    fora do formato da RN-05 e data de expiração no passado
  - 409 para código próprio já em uso (RN-01)
- GET /{code}
  - 302 para a URL de destino, incrementando o contador
  - 404 se o código não existe
  - 410 se o link expirou, sem incrementar o contador
- GET /api/links/{code}/stats
  - público, sem autenticação (RF-07)
  - 200 com code, accessCount, createdAtUtc, expiresAt
  - 404 se o código não existe

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora
- Validação de URL pública sem DNS (D-06) aceita hostnames que resolvem para
  endereços privados; aceito por ora, porque a API só redireciona e não
  acessa a URL de destino
- Migrações aplicadas na inicialização da API servem para uma única instância;
  com várias instâncias, devem rodar como etapa do deploy para evitar corrida
- Cache de redirecionamento fica fora de escopo: sem requisito que o peça, e
  arriscaria a contagem de acessos (RF-05) e a expiração (RN-03)

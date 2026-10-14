# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar solução, projetos (API e testes) e estrutura de pastas | Constituição | | Solução compila e `dotnet test` roda |
| T-02 | Modelar a entidade Link, o DbContext e a migração | RF-01, RF-08, D-01, D-04 | T-01 | Migração aplica no banco com índice único em Code |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, RN-05, D-03 | T-01 | Teste unitário cobre tamanho e alfabeto A-Z e 1-9, sem 0 |
| T-04 | Validar URL de destino | RN-02, D-06, borda 2048 | T-01 | Teste unitário cobre http, https, esquemas inválidos, URL relativa, mais de 2048 caracteres e hosts não públicos |
| T-05 | Validar código próprio | RF-03, RN-05 | T-01 | Teste unitário cobre 4 e 20 caracteres, fora do tamanho, 0, minúsculas, espaços e especiais |
| T-06 | Validar data de expiração | RF-06, D-07, D-08 | T-01 | Teste unitário cobre data passada recusada, hoje aceita e validade até o fim da data em UTC |
| T-07 | Criar link com código automático | RF-01, RN-06, D-05 | T-02, T-03, T-04 | Teste de integração cria e retorna 201; teste unitário cobre nova tentativa em colisão |
| T-08 | Aceitar código próprio com unicidade | RF-03, RN-01, D-04 | T-05, T-07 | Teste de integração com código próprio retorna 201; duplicado retorna 409 |
| T-09 | Aceitar data de expiração na criação | RF-06 | T-06, T-07 | Teste de integração com data passada retorna 400 com ProblemDetails |
| T-10 | Redirecionar por código | RF-04, D-09 | T-07 | Teste de integração retorna 302; código inexistente retorna 404 |
| T-11 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-10 | Teste concorrente não perde contagem |
| T-12 | Bloquear link expirado | RN-03, RF-06, D-07 | T-09, T-11 | Teste de integração: expirado retorna 410 sem contar; último instante do dia ainda redireciona |
| T-13 | Endpoint público de estatísticas | RF-07 | T-07 | Teste de integração retorna 200 com o total; código inexistente retorna 404 |
| T-14 | Persistência após reinício | RF-08 | T-10 | Teste de integração recria o host sobre o mesmo arquivo SQLite e o link continua redirecionando |

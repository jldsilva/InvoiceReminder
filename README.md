# InvoiceReminder

Sistema para envio automático de lembretes de pagamento de boletos bancários.

## Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Funcionalidades](#funcionalidades)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Pré-requisitos](#pré-requisitos)
- [Instalação](#instalação)
- [Configuração](#configuração)
- [Como Usar](#como-usar)
- [Contribuição](#contribuição)
- [Licença](#licença)
- [Contato](#contato)

---

## Sobre o Projeto

O **InvoiceReminder** é um sistema desenvolvido em C# para automatizar o envio de lembretes referentes a pagamentos de boletos bancários (invoices). Ele pode ser utilizado por empresas ou profissionais autônomos que desejam organizar e automatizar o processo de pagamentos, evitando atrasos e otimizando o fluxo de caixa.

## Funcionalidades

- Cadastro de clientes e emitentes de cobrança
- Agendamento de lembretes automáticos via Telegram chat bot
- Personalização de mensagens de lembrete
- Relatórios de boletos pendentes e lembretes enviados
- Suporte a múltiplos usuários
- Interface Rest API

## Tecnologias Utilizadas

- **Linguagem:** C#
- **Framework:** .NET Core 10
- **Autenticação:** JSON Web Token
- **Banco de Dados:** PostgreSQL
- **Solução de ORM:** Entity Framework Core e Dapper
- **Serviços Externos:** Google Api, iText, Telegram.Bot
- **Agendamento de Tarefas:** Quartz.net
- **Testes Unitários:** Bogus, MSTest, NSubstitute e Shouldly
- **Testes de Integração:** Bogus, MSTest, Shouldly e Testcontainers
- **Testes de Arquitetura:** NetArchTest

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (versão recomendada: 10.0 ou superior)
- [PostgreSQL](https://www.postgresql.org/download/) (versão recomendada: 16 ou superior)
- [Google OAuth Client](https://console.cloud.google.com/) para gerar chave de integração com o sistema de autenticação sem senha
- Criar um chat bot do Telegram através da interação com @BotFather
- Acesso à internet para recuperação de e-mails e posterior envio de mensagens

## Instalação

Clone o repositório:

```bash
git clone https://github.com/jldsilva/InvoiceReminder.git
cd InvoiceReminder
```

Restaure os pacotes NuGet e compile o projeto:

```bash
dotnet restore
dotnet build
```

## Configuração

Antes de executar, configure os parâmetros de banco de dados e outros no arquivo de configuração (exemplo: `appsettings.json`):

```json
{
  "ProviderName": "Npgsql",
  "appKeys": {
    "googleOauthClientId": "CLIENT_ID",
    "googleOauthClientSecret": "CLIENT_SECRET",
    "telegramBotToken": "BOT_TOKEN"
  },
  "ConnectionStrings": {
    "DatabaseConnection": "CONNECTION_STRING"
  },
  "JwtOptions": {
    "Issuer": "ISSUER",
    "Audience": "AUDIENCE",
    "SecretKey": "SECRET_KEY"
  }
}
```

> **Nota:** Nunca compartilhe suas credenciais em repositórios públicos dê preferência ao recurso de user secrets do Visual Studio ou VS Code.

## Como Usar

Execute o projeto:

```bash
dotnet run
```

Em ambiente development, a interface Rest API Scalar estará disponível para uso.
Siga as instruções exibidas na tela para cadastrar clientes, notas fiscais e configurar os lembretes.

### Exemplo de uso (CLI):

```bash
# Adicionar cliente
curl https://localhost:7104/api/user \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
  "id": "",
  "name": null,
  "email": null,
  "password": null
}'

# Adicionar um scan de boleto
curl https://localhost:7104/api/scan_email \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
  "id": "",
  "userId": "",
  "invoiceType": 1,
  "beneficiary": null,
  "description": null,
  "senderEmailAddress": null,
  "attachmentFileName": null
}'

# Adicionar um job_schedule
curl https://localhost:7104/api/job_schedule \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{
  "id": "",
  "userId": "",
  "cronExpression": null
}'
```

## Contato

Feito por [jldsilva](https://github.com/jldsilva) - Entre em contato para dúvidas ou sugestões!

---
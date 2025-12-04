# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=64198
USER ${APP_UID}
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Instala a biblioteca necessária para evitar erro de libgssapi_krb5.so.2
USER root
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER ${APP_UID}

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Adiciona o arquivo de versões centralizadas
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

WORKDIR /src
COPY ["InvoiceReminder.API/InvoiceReminder.API.csproj", "InvoiceReminder.API/"]
COPY ["InvoiceReminder.Application/InvoiceReminder.Application.csproj", "InvoiceReminder.Application/"]
COPY ["InvoiceReminder.Data/InvoiceReminder.Data.csproj", "InvoiceReminder.Data/"]
COPY ["InvoiceReminder.Domain/InvoiceReminder.Domain.csproj", "InvoiceReminder.Domain/"]
COPY ["InvoiceReminder.JobScheduler/InvoiceReminder.JobScheduler.csproj", "InvoiceReminder.JobScheduler/"]
COPY ["InvoiceReminder.ExternalServices/InvoiceReminder.ExternalServices.csproj", "InvoiceReminder.ExternalServices/"]
COPY ["InvoiceReminder.Authentication/InvoiceReminder.Authentication.csproj", "InvoiceReminder.Authentication/"]
COPY ["InvoiceReminder.CrossCutting.IoC/InvoiceReminder.CrossCutting.IoC.csproj", "InvoiceReminder.CrossCutting.IoC/"]
RUN dotnet restore "./InvoiceReminder.API/InvoiceReminder.API.csproj"
COPY . .
WORKDIR "/src/InvoiceReminder.API"
RUN dotnet build "./InvoiceReminder.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InvoiceReminder.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InvoiceReminder.API.dll"]

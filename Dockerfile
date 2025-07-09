# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia arquivos e restaura dependências
COPY *.csproj ./
RUN dotnet restore

RUN mkdir -p /data

# Copia o restante e publica
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Expõe a porta padrão do ASP.NET
EXPOSE 80

ENTRYPOINT ["dotnet", "backend.dll"]
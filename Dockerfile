# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o projeto e restaura as dependências
COPY ALLmoco.csproj ./
RUN dotnet restore

# Copia o restante do código
COPY . .

# Publica a aplicação
RUN dotnet publish ALLmoco.csproj -c Release -o /app/publish --no-restore

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# O Render define a porta pela variável PORT
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ALLmoco.dll"]
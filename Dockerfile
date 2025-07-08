# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copia todo desde la raíz (SchoolManager.sln, .csproj, etc.)
COPY . .

# Restaurar dependencias
RUN dotnet restore

# Publicar la app (usando la solución real)
RUN dotnet publish SchoolManager.sln -c Release -o /out

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "SchoolManager.dll"]

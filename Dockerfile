# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copiar la solución y el proyecto
COPY EduPlanner.sln .
COPY SchoolManager/SchoolManager.csproj ./SchoolManager/

# Restaurar dependencias
RUN dotnet restore

# Copiar el resto del código
COPY . .

# Publicar la aplicación
WORKDIR /app/SchoolManager
RUN dotnet publish -c Release -o /out

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

# Expone el puerto por defecto
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Comando para iniciar la app
ENTRYPOINT ["dotnet", "SchoolManager.dll"]
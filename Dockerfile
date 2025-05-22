# Usar la imagen base de .NET SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar dependencias
COPY ["SchoolManager.csproj", "./"]
RUN dotnet restore

# Copiar el resto del código
COPY . .

# Publicar la aplicación
RUN dotnet publish -c Release -o /app/publish

# Construir la imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configurar las variables de entorno
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "SchoolManager.dll"] 
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all csproj files first for layer-cached restore
COPY ["EasyLoginAPI/EasyLoginAPI.csproj", "EasyLoginAPI/"]
COPY ["EasyLogin.Domain/EasyLogin.Domain.csproj", "EasyLogin.Domain/"]
COPY ["EasyLogin.Application/EasyLogin.Application.csproj", "EasyLogin.Application/"]
COPY ["EasyLogin.Infrastructure/EasyLogin.Infrastructure.csproj", "EasyLogin.Infrastructure/"]
RUN dotnet restore "EasyLoginAPI/EasyLoginAPI.csproj"

COPY . .
WORKDIR "/src/EasyLoginAPI"
RUN dotnet publish "EasyLoginAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EasyLoginAPI.dll"]

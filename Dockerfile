FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SpeakHalo.API/SpeakHalo.API.csproj", "SpeakHalo.API/"]
COPY ["SpeakHalo.Application/SpeakHalo.Application.csproj", "SpeakHalo.Application/"]
COPY ["SpeakHalo.Domain/SpeakHalo.Domain.csproj", "SpeakHalo.Domain/"]
COPY ["SpeakHalo.Infrastructure/SpeakHalo.Infrastructure.csproj", "SpeakHalo.Infrastructure/"]
COPY ["SpeakHalo.Shared/SpeakHalo.Shared.csproj", "SpeakHalo.Shared/"]
RUN dotnet restore "SpeakHalo.API/SpeakHalo.API.csproj"
COPY . .
WORKDIR "/src/SpeakHalo.API"
RUN dotnet build "SpeakHalo.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpeakHalo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpeakHalo.API.dll"]

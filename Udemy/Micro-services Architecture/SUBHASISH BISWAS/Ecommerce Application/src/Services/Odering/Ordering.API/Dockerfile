#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Services/Odering/Ordering.API/Ordering.API.csproj", "Services/Odering/Ordering.API/"]
COPY ["Services/Odering/Ordering.Application/Ordering.Application.csproj", "Services/Odering/Ordering.Application/"]
COPY ["Services/Odering/Ordering.Domain/Ordering.Domain.csproj", "Services/Odering/Ordering.Domain/"]
COPY ["BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "BuildingBlocks/EventBus.Messages/"]
COPY ["Services/Odering/Ordering.Infrastructure/Ordering.Infrastructure.csproj", "Services/Odering/Ordering.Infrastructure/"]
RUN dotnet restore "Services/Odering/Ordering.API/Ordering.API.csproj"
COPY . .
WORKDIR "/src/Services/Odering/Ordering.API"
RUN dotnet build "Ordering.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ordering.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ordering.API.dll"]
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Services/Odering/Odering.API/Odering.API.csproj", "Services/Odering/Odering.API/"]
COPY ["Services/Odering/Ordering.Application/Ordering.Application.csproj", "Services/Odering/Ordering.Application/"]
COPY ["Services/Odering/Ordering.Domain/Ordering.Domain.csproj", "Services/Odering/Ordering.Domain/"]
COPY ["Services/Odering/Ordering.Infrastructure/Ordering.Infrastructure.csproj", "Services/Odering/Ordering.Infrastructure/"]
RUN dotnet restore "Services/Odering/Odering.API/Odering.API.csproj"
COPY . .
WORKDIR "/src/Services/Odering/Odering.API"
RUN dotnet build "Odering.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Odering.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Odering.API.dll"]
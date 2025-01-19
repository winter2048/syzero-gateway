FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 729
COPY . .
ENTRYPOINT ["dotnet", "SyZero.Gateway.dll"]
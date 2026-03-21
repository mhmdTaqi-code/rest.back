FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore backend/SmartDiningSystem.Api/SmartDiningSystem.Api.csproj
RUN dotnet publish backend/SmartDiningSystem.Api/SmartDiningSystem.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SmartDiningSystem.Api.dll"]

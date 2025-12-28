# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore AgriProcurement.sln
RUN dotnet publish src/Modules/Procurement/Procurement.Api/Procurement.Api.csproj -c Release -o /out /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Procurement.Api.dll"]

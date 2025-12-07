#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
LABEL org.opencontainers.image.source="https://github.com/PaulTrampert/PTrampert.SpaHost"
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PTrampert.SpaHost/PTrampert.SpaHost.csproj", "PTrampert.SpaHost/"]
RUN dotnet restore "PTrampert.SpaHost/PTrampert.SpaHost.csproj"
COPY . .
WORKDIR "/src/PTrampert.SpaHost"
RUN dotnet build "PTrampert.SpaHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PTrampert.SpaHost.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PTrampert.SpaHost.dll"]
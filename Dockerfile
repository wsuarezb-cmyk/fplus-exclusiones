FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BlazorS7Upload/BlazorS7Upload.csproj", "BlazorS7Upload/"]
RUN dotnet restore "BlazorS7Upload/BlazorS7Upload.csproj"
COPY . .
RUN dotnet publish "BlazorS7Upload/BlazorS7Upload.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlazorS7Upload.dll"]
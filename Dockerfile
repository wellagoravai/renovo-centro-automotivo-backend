FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/RenovoWorkshop.sln
RUN dotnet publish backend/RenovoWorkshop.Api/RenovoWorkshop.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["sh", "-c", "dotnet RenovoWorkshop.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Auth.sln ./
COPY Auth/Api/Api.csproj                         Auth/Api/
COPY Auth/Application/Application.csproj         Auth/Application/
COPY Auth/Domain/Domain.csproj                   Auth/Domain/
COPY Auth/Infrastructure/Infrastructure.csproj   Auth/Infrastructure/
COPY Auth/Protos/Protos.csproj                   Auth/Protos/

RUN dotnet restore Auth.sln

COPY . .
RUN dotnet publish Auth/Api/Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]

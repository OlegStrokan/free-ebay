# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Auth.sln ./
COPY src/Api/Api.csproj                         src/Api/
COPY src/Application/Application.csproj         src/Application/
COPY src/Domain/Domain.csproj                   src/Domain/
COPY src/Infrastructure/Infrastructure.csproj   src/Infrastructure/
COPY src/Protos/Protos.csproj                   src/Protos/

RUN dotnet restore Auth.sln

COPY . .
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

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

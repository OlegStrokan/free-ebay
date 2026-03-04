# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Order.sln ./
COPY Order/Api/Api.csproj                                         Order/Api/
COPY Order/Application/Application.csproj                         Order/Application/
COPY Order/Domain/Domain.csproj                                   Order/Domain/
COPY Order/Infrastructure/Infrastructure.csproj                   Order/Infrastructure/
COPY Order/Protos/Protos.csproj                                   Order/Protos/

RUN dotnet restore Order.sln

COPY . .
RUN dotnet publish Order/Api/Api.csproj -c Release -o /app/publish --no-restore

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

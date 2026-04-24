#  Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY User.sln ./
COPY User/Api/Api.csproj                         User/Api/
COPY User/Application/Application.csproj         User/Application/
COPY User/Domain/Domain.csproj                   User/Domain/
COPY User/Infrastructure/Infrastructure.csproj   User/Infrastructure/
COPY User/Protos/Protos.csproj                   User/Protos/

RUN dotnet restore User.sln

COPY . .
RUN dotnet publish User/Api/Api.csproj -c Release -o /app/publish --no-restore

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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CodeReview.API/CodeReview.API.csproj", "CodeReview.API/"]
RUN dotnet restore "CodeReview.API/CodeReview.API.csproj"
COPY . .
WORKDIR "/src/CodeReview.API"
RUN dotnet publish "CodeReview.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CodeReview.API.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FormBuilderAPI/FormBuilderAPI.csproj", "FormBuilderAPI/"]
RUN dotnet restore "FormBuilderAPI/FormBuilderAPI.csproj"
COPY . .
WORKDIR "/src/FormBuilderAPI"
RUN dotnet build "FormBuilderAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FormBuilderAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FormBuilderAPI.dll"]
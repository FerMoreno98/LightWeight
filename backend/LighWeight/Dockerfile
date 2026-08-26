FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY LightWeight.slnx ./
COPY src/App/LightWeight.App/*.csproj ./App/LightWeight.App/

# --- Módulo: auth ---
COPY src/Modules/auth/LightWeight.Auth.Domain/*.csproj ./Modules/auth/LightWeight.Auth.Domain/
COPY src/Modules/auth/LightWeight.Auth.Application/*.csproj ./Modules/auth/LightWeight.Auth.Application/
COPY src/Modules/auth/LightWeight.Auth.Infrastructure/*.csproj ./Modules/auth/LightWeight.Auth.Infrastructure/
COPY src/Modules/auth/LightWeight.Auth.Api/*.csproj ./Modules/auth/LightWeight.Auth.Api/

# --- Módulo: bodymetrics ---
COPY src/Modules/bodymetrics/LightWeight.BodyMetrics.Domain/*.csproj ./Modules/bodymetrics/LightWeight.BodyMetrics.Domain/
COPY src/Modules/bodymetrics/LightWeight.BodyMetrics.Application/*.csproj ./Modules/bodymetrics/LightWeight.BodyMetrics.Application/
COPY src/Modules/bodymetrics/LightWeight.BodyMetrics.Infrastructure/*.csproj ./Modules/bodymetrics/LightWeight.BodyMetrics.Infrastructure/
COPY src/Modules/bodymetrics/LightWeight.BodyMetrics.Api/*.csproj ./Modules/bodymetrics/LightWeight.BodyMetrics.Api/

# --- Módulo: userprofile ---
COPY src/Modules/user-profile/LightWeight.UserProfile.Domain/*.csproj ./Modules/user-profile/LightWeight.UserProfile.Domain/
COPY src/Modules/user-profile/LightWeight.UserProfile.Application/*.csproj ./Modules/user-profile/LightWeight.UserProfile.Application/
COPY src/Modules/user-profile/LightWeight.UserProfile.Infrastructure/*.csproj ./Modules/user-profile/LightWeight.UserProfile.Infrastructure/
COPY src/Modules/user-profile/LightWeight.UserProfile.Api/*.csproj ./Modules/user-profile/LightWeight.UserProfile.Api/

# --- Módulo: training ---
COPY src/Modules/training/LightWeight.Training.Domain/*.csproj ./Modules/training/LightWeight.Training.Domain/
COPY src/Modules/training/LightWeight.Training.Application/*.csproj ./Modules/training/LightWeight.Training.Application/
COPY src/Modules/training/LightWeight.Training.Infrastructure/*.csproj ./Modules/training/LightWeight.Training.Infrastructure/
COPY src/Modules/training/LightWeight.Training.Api/*.csproj ./Modules/training/LightWeight.Training.Api/

# --- Módulo: neat ---
COPY src/Modules/neat/LightWeight.Neat.Domain/*.csproj ./Modules/neat/LightWeight.Neat.Domain/
COPY src/Modules/neat/LightWeight.Neat.Application/*.csproj ./Modules/neat/LightWeight.Neat.Application/
COPY src/Modules/neat/LightWeight.Neat.Infrastructure/*.csproj ./Modules/neat/LightWeight.Neat.Infrastructure/
COPY src/Modules/neat/LightWeight.Neat.Api/*.csproj ./Modules/neat/LightWeight.Neat.Api/

# --- Módulo: nutrition ---
COPY src/Modules/nutrition/LightWeight.Nutrition.Domain/*.csproj ./Modules/nutrition/LightWeight.Nutrition.Domain/
COPY src/Modules/nutrition/LightWeight.Nutrition.Application/*.csproj ./Modules/nutrition/LightWeight.Nutrition.Application/
COPY src/Modules/nutrition/LightWeight.Nutrition.Infrastructure/*.csproj ./Modules/nutrition/LightWeight.Nutrition.Infrastructure/
COPY src/Modules/nutrition/LightWeight.Nutrition.Api/*.csproj ./Modules/nutrition/LightWeight.Nutrition.Api/

# --- Shared ---
COPY src/shared/*.csproj ./shared/ 

RUN dotnet restore LightWeight.slnx

COPY src/ ./
RUN dotnet publish App/LightWeight.App/LightWeight.App.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Usuario no root (buena práctica de seguridad)
RUN useradd -m appuser
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LightWeight.App.dll"]
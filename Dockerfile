#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# See https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/
# 8.0-preview-alpine works too
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

# copy everything else and build app
COPY . .
RUN dotnet publish -a $TARGETARCH -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

USER $APP_UID
ENTRYPOINT ["./Web"]


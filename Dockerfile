# Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source
COPY . .
RUN dotnet publish -r linux-musl-x64 --output "./dist" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Run
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64

EXPOSE 8080

RUN mkdir /app
WORKDIR /app
COPY ./dist/. ./

RUN chmod +x ./Greyboard
CMD ["./Greyboard", "--urls", "http://0.0.0.0:8080"]

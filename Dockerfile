FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine-amd64

EXPOSE 8080

RUN mkdir /app
WORKDIR /app
COPY ./dist/. ./

RUN chmod +x ./Greyboard
CMD ["./Greyboard", "--urls", "http://0.0.0.0:8080"]

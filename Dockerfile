FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS restore
WORKDIR /app

ENV NODE_VERSION 10.15.3
ENV NODE_DOWNLOAD_SHA 6c35b85a7cd4188ab7578354277b2b2ca43eacc864a2a16b3669753ec2369d52
RUN curl -SL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs

COPY package*.json ./

RUN npm install && \
    npm install -g --unsafe-perm node-sass

ARG BUILD_CONFIG="Release"

RUN mkdir -p /app/vsdbg && touch /app/vsdbg/touched
ENV DEBIAN_FRONTEND noninteractive
RUN if [ "${BUILD_CONFIG}" = "Debug" ]; then \
    apt-get update && \
    apt-get install apt-utils -y --no-install-recommends && \
    apt-get install curl unzip -y && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /app/vsdbg; \
    fi
ENV DEBIAN_FRONTEND teletype

ARG MYGET_API_KEY

COPY *.sln ./
COPY ./src/Ranger.Identity/Ranger.Identity.csproj ./src/Ranger.Identity/Ranger.Identity.csproj
COPY ./scripts ./scripts

RUN ./scripts/create-nuget-config.sh ${MYGET_API_KEY}
RUN dotnet restore

COPY ./src ./src
COPY ./test ./test

RUN npm run scss
RUN dotnet publish -c ${BUILD_CONFIG} -o /app/published --no-restore

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=restore /app/published .
COPY --from=restore /app/vsdbg ./vsdbg

ARG BUILD_CONFIG="Release"
ARG ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ARG DOCKER_IMAGE_TAG="NO_TAG"
ENV DOCKER_IMAGE_TAG=${DOCKER_IMAGE_TAG}

ENV DEBIAN_FRONTEND noninteractive
RUN if [ "${BUILD_CONFIG}" = "Debug" ]; then \
    apt-get update && \
    apt-get install procps -y; \
    fi
ENV DEBIAN_FRONTEND teletype

EXPOSE 5000
ENTRYPOINT ["dotnet", "Ranger.Identity.dll"]
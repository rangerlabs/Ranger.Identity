#!/bin/bash
DOCKER_TAG=''

case "$TRAVIS_BRANCH" in
  "master")
    DOCKER_TAG=2.0.$TRAVIS_BUILD_NUMBER
    ;;
esac

docker build -t rangerlabs/ranger.identity:$DOCKER_TAG --build-arg MYGET_API_KEY=$MYGET_KEY --build-arg DOCKER_IMAGE_TAG=$DOCKER_TAG .
#!/bin/bash
DOCKER_TAG=''

if [ $TRAVIS_EVENT_TYPE != "pull_request" ]; then
  case "$TRAVIS_BRANCH" in
    "master")
      DOCKER_TAG=2.0.$TRAVIS_BUILD_NUMBER
      ;;
    "dev")
      DOCKER_TAG=dev
      ;;    
  esac

  docker login -u $DOCKER_USERNAME -p $DOCKER_PASSWORD
  docker push rangerlabs/ranger.identity:$DOCKER_TAG
fi
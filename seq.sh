#!/bin/bash

docker run \
  --rm \
  -e ACCEPT_EULA=Y \
  -v $HOST_PATH_TO_SEQ:/data \
  -p 5341:80 \
  datalust/seq:latest
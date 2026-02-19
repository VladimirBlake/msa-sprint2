#!/bin/bash

RETRIES=100

kubectl run curlpod --rm -it --image=curlimages/curl --restart=Never -- /bin/sh -c "
echo 'Testing canary feature from inside cluster...';
for i in \$(seq 1 $RETRIES); do
  curl -s -H 'Host: booking-service' http://booking-service/feature;
  echo;   # add a newline after each response
done | sort | uniq -c
"
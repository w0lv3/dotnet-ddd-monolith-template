#!/usr/bin/env sh
set -eu

client_id="examplelocalclient"
pool_id="us-east-1_examplepool"
scope="example-api/examples.read"
localstack_port=${LOCALSTACK_PORT:-4566}
api_port=${API_PORT:-5075}

client_secret=$(docker compose -f docker-compose.local.yml \
    exec -T localstack awslocal cognito-idp describe-user-pool-client \
    --user-pool-id "$pool_id" \
    --client-id "$client_id" \
    --query UserPoolClient.ClientSecret \
    --output text)
access_token=$(curl --fail --silent --show-error \
    --user "$client_id:$client_secret" \
    --data grant_type=client_credentials \
    --data scope="$scope" \
    "http://cognito-idp.localhost.localstack.cloud:$localstack_port/_aws/cognito-idp/oauth2/token" |
    jq -er '.access_token')

curl --fail --silent --show-error \
    --header "Authorization: Bearer $access_token" \
    "http://localhost:$api_port/api/examples"

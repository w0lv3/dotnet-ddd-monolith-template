#!/usr/bin/env sh
set -eu

region="us-east-1"
pool_id="us-east-1_examplepool"
client_id="examplelocalclient"
resource_server="example-api"
username="developer@example.local"

if awslocal cognito-idp describe-user-pool --user-pool-id "$pool_id" >/dev/null 2>&1; then
    exit 0
fi

awslocal cognito-idp create-user-pool \
    --pool-name example-local \
    --user-pool-tags "_custom_id_=$pool_id" \
    --region "$region" >/dev/null

awslocal cognito-idp create-resource-server \
    --user-pool-id "$pool_id" \
    --identifier "$resource_server" \
    --name "Example API" \
    --scopes '[{"ScopeName":"examples.read","ScopeDescription":"Read examples"},{"ScopeName":"examples.write","ScopeDescription":"Write examples"}]' \
    --region "$region" >/dev/null

awslocal cognito-idp create-user-pool-client \
    --user-pool-id "$pool_id" \
    --client-name "_custom_id_:$client_id" \
    --generate-secret \
    --allowed-o-auth-flows-user-pool-client \
    --allowed-o-auth-flows client_credentials \
    --allowed-o-auth-scopes "$resource_server/examples.read" "$resource_server/examples.write" \
    --region "$region" >/dev/null

awslocal cognito-idp create-group \
    --user-pool-id "$pool_id" \
    --group-name examples.read \
    --region "$region" >/dev/null
awslocal cognito-idp create-group \
    --user-pool-id "$pool_id" \
    --group-name examples.write \
    --region "$region" >/dev/null

awslocal cognito-idp admin-create-user \
    --user-pool-id "$pool_id" \
    --username "$username" \
    --temporary-password 'Developer123!' \
    --user-attributes Name=email,Value="$username" Name=email_verified,Value=true \
    --message-action SUPPRESS \
    --region "$region" >/dev/null
awslocal cognito-idp admin-set-user-password \
    --user-pool-id "$pool_id" \
    --username "$username" \
    --password 'Developer123!' \
    --permanent \
    --region "$region"
awslocal cognito-idp admin-add-user-to-group \
    --user-pool-id "$pool_id" \
    --username "$username" \
    --group-name examples.read \
    --region "$region"

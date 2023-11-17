param location string = 'canadaeast'
param cognitivesearch_name string = 'lilaoai512'
param cognitiveserviceaccount_name string = 'lilaoai512'

resource cognitiveserviceaccount_name_resource 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: cognitiveserviceaccount_name
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: cognitiveserviceaccount_name
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource cognitivesearch_name_resource 'Microsoft.Search/searchServices@2022-09-01' = {
  name: cognitivesearch_name
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    networkRuleSet: {
      ipRules: []
    }
    encryptionWithCmk: {
      enforcement: 'Unspecified'
    }
    disableLocalAuth: false
    authOptions: {
      apiKeyOnly: {}
    }
  }
}

resource cognitiveserviceaccount_name_ada 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: cognitiveserviceaccount_name_resource
  name: 'ada'
  sku: {
    name: 'Standard'
    capacity: 120
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.Default'
  }
}

resource cognitiveserviceaccount_name_gpt4 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: cognitiveserviceaccount_name_resource
  name: 'gpt4'
  dependsOn: [
    cognitiveserviceaccount_name_ada
  ]
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4'
      version: '0613'
    }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
    raiPolicyName: 'Microsoft.Default'
  }
}

output aoai_endpoint string = cognitiveserviceaccount_name_resource.properties.endpoint
#disable-next-line outputs-should-not-contain-secrets
output aoai_apikey string = cognitiveserviceaccount_name_resource.listKeys().key1
output chatcompletion_deploymentname string = 'gp4'
output embedding_deploymentname string = 'ada'
#disable-next-line outputs-should-not-contain-secrets
output cognitivesearch_apikey string = cognitivesearch_name_resource.listAdminKeys().primaryKey
output cognitivesearch_endpoint string = 'https://${cognitivesearch_name}search.windows.net'

// SQL Database Import
resource sqlServerDatabase 'Microsoft.Sql/servers/databases@2014-04-01' = {
  name: /*${1:'name'}*/'name'
  location: /*${2:'location'}*/'location'
}

resource /*${3:sqlDatabaseImport}*/sqlDatabaseImport 'Microsoft.Sql/servers/databases/extensions@2014-04-01' = {
  parent: sqlServerDatabase
  name: /*${4:'name'}*/'name'
  properties: {
    storageKeyType: /*'${5|StorageAccessKey,SharedAccessKey|}'*/'StorageAccessKey'
    storageKey: /*${6:'storageKey'}*/'storageKey'
    storageUri: /*${7:'storageUri'}*/'storageUri'
    administratorLogin: /*${8:'administratorLogin'}*/'administratorLogin'
    administratorLoginPassword: /*${9:'administratorLoginPassword'}*/'administratorLoginPassword'
    operationMode: /*${10:'operationMode'}*/'operationMode'
  }
}

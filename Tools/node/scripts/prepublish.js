const package = require('../package.json')
const path = require('path')
const fs = require('fs')

const argv = process.argv.slice(2)

// Update version field in package.json using script argument
package.version = argv[0]
fs.writeFileSync(path.resolve(__dirname, '../package.json'), JSON.stringify(package, null, 2), 'utf8')

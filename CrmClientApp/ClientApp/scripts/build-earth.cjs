const fs = require('fs');
const path = require('path');

// Find the earth package in node_modules and check if it's already built
// This script just verifies the package is available, it doesn't build it
// Building should be handled by the package's own postinstall or manually

const nodeModulesPath = path.join(process.cwd(), 'node_modules');

// Check multiple possible locations
const possiblePaths = [
  path.join(nodeModulesPath, '@frankcrum', 'earth', 'packages', 'earth', 'dist', 'index.css'),
  path.join(nodeModulesPath, '@frankcrum', 'earth', 'dist', 'index.css'),
];

let found = false;
for (const cssPath of possiblePaths) {
  if (fs.existsSync(cssPath)) {
    found = true;
    break;
  }
}

if (!found) {
  // Try to find the package directory
  const earthSymlink = path.join(nodeModulesPath, '@frankcrum', 'earth');
  if (fs.existsSync(earthSymlink)) {
    try {
      const realPath = fs.realpathSync(earthSymlink);
      const earthPackagePath = path.join(realPath, 'packages', 'earth');
      const distPath = path.join(earthPackagePath, 'dist', 'index.css');
      
      if (fs.existsSync(distPath)) {
        found = true;
      }
    } catch (error) {
      // Ignore errors - package structure might be different
    }
  }
  
  if (!found) {
    console.warn('⚠️  @frankcrum/earth CSS not found. The package may need to be built.');
    console.warn('   If you encounter CSS import errors, you may need to build the earth package manually.');
    console.warn('   This is a non-blocking warning - installation will continue.');
  }
}

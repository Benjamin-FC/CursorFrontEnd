const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Find the earth package in node_modules
const nodeModulesPath = path.join(process.cwd(), 'node_modules');
const earthSymlink = path.join(nodeModulesPath, '@frankcrum', 'earth');

if (fs.existsSync(earthSymlink)) {
  const realPath = fs.realpathSync(earthSymlink);
  const earthPackagePath = path.join(realPath, 'packages', 'earth');
  
  if (fs.existsSync(earthPackagePath)) {
    console.log('Building @frankcrum/earth package...');
    try {
      process.chdir(earthPackagePath);
      // Check if dist exists, if not build it
      const distPath = path.join(earthPackagePath, 'dist');
      if (!fs.existsSync(distPath)) {
        execSync('pnpm install', { stdio: 'inherit' });
        execSync('pnpm run build', { stdio: 'inherit' });
      } else {
        console.log('@frankcrum/earth already built');
      }
    } catch (error) {
      console.warn('Could not build @frankcrum/earth:', error.message);
      // Continue anyway
    }
  }
}

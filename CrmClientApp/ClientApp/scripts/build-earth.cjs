const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Find the earth package in node_modules
const nodeModulesPath = path.join(process.cwd(), 'node_modules');
const earthSymlink = path.join(nodeModulesPath, '@frankcrum', 'earth');

if (fs.existsSync(earthSymlink)) {
  try {
    const realPath = fs.realpathSync(earthSymlink);
    const earthPackagePath = path.join(realPath, 'packages', 'earth');
    const distPath = path.join(earthPackagePath, 'dist', 'index.css');
    
    // Check if dist/index.css exists, if not try to build it
    if (!fs.existsSync(distPath)) {
      console.log('Building @frankcrum/earth package...');
      try {
        const originalCwd = process.cwd();
        process.chdir(earthPackagePath);
        
        // Try to build, but don't fail if it doesn't work
        try {
          execSync('pnpm install', { stdio: 'pipe', timeout: 30000 });
          execSync('pnpm run build', { stdio: 'pipe', timeout: 60000 });
        } catch (buildError) {
          console.warn('Could not build @frankcrum/earth automatically:', buildError.message);
          console.warn('You may need to build it manually or the package may already be built');
        }
        
        process.chdir(originalCwd);
      } catch (error) {
        console.warn('Could not build @frankcrum/earth:', error.message);
        // Continue anyway - the package might already be built
      }
    } else {
      console.log('@frankcrum/earth already built');
    }
  } catch (error) {
    // Silently continue if we can't find or build the package
    // It might already be built or the structure might be different
  }
}

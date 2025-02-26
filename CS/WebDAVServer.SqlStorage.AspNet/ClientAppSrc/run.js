import { argv } from "process";
import fs from "fs";
import { execSync } from "child_process";

const isNetFramework = argv[2] === "true" ? true : false;

console.log("npm version:");
execSync("npm -v", { stdio: "inherit" });

console.log("node version:");
execSync("node -v", { stdio: "inherit" });

let currVersion, newVersion, newBetaVersion;

if (fs.existsSync("./node_modules/webdav.client")) {
  const packageJson = await import("webdav.client/package.json", {
    with: { type: "json" },
  });
  currVersion = packageJson.default.version;
  newVersion = execSync("npm show webdav.client version").toString().trim();
  newBetaVersion = execSync("npm show webdav.client@beta version")
    .toString()
    .trim();
}

function installAndBuild(isBeta = false) {
  const packageName = `webdav.client${isBeta ? "@beta" : ""}`;
  console.log(`Installing ${packageName}...`);
  execSync(`npm install ${packageName} --save`, { stdio: "inherit" });
  buildApp();
  copyClient();
}

function buildApp() {
  const buildCommand = isNetFramework
    ? "npm run build:netframework"
    : "npm run build";
  console.log(`Running build: ${buildCommand}`);
  execSync(buildCommand, { stdio: "inherit" });
}

function copyClient() {
  const targetDir = "../wwwroot/webdav.client";
  if (!fs.existsSync(targetDir)) {
    fs.mkdirSync(targetDir, { recursive: true });
  }

  console.log("Copying webdav.client files...");
  execSync(
    `(robocopy node_modules/webdav.client ${targetDir} /E) ^& IF %ERRORLEVEL% LEQ 1 exit 0`,
    { stdio: "inherit" }
  );
}

if (!currVersion) {
  console.log("webdav.client is not installed. Installing...");
  installAndBuild();
} else if (currVersion === newVersion || currVersion === newBetaVersion) {
  console.log(`webdav.client version ${currVersion} is up to date.`);
  if (!fs.existsSync("../wwwroot/app.js")) {
    installAndBuild(currVersion.includes("beta"));
  }
} else {
  const isBeta = currVersion.includes("beta");
  console.log(
    `Found new webdav.client version: ${isBeta ? newBetaVersion : newVersion}`
  );
  installAndBuild(isBeta);
}

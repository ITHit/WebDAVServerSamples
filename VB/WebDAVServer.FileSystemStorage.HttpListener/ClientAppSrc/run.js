import { argv } from "process";
import fs from "fs";
import { execSync } from "child_process";

let isNetFramework = argv[2] === "true" ? true : false;

// Show npm version
console.log("npm version: ");
execSync("npm -v", { stdio: "inherit" });

// Show node version
console.log("node version: ");
execSync("node -v", { stdio: "inherit" });

let currVersion;
let newVersion;
let newBetaVersion;

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

if (newVersion === currVersion) {
  console.log("webdav.client version is up to date");
  if (currVersion == undefined) {
    execSync("npm install webdav.client --save", { stdio: "inherit" });
    installAndBuild();
  }

  if (!fs.existsSync("../wwwroot/app.js")) {
    installAndBuild();
  }
} else if (newBetaVersion === currVersion) {
  console.log("webdav.client version is up to date");
  if (currVersion == undefined) {
    execSync("npm install webdav.client@beta --save", { stdio: "inherit" });
    installAndBuild();
  }

  if (!fs.existsSync("../wwwroot/app.js")) {
    installAndBuild();
  }
} else {
  let isBeta = currVersion.includes("beta");
  let newV = isBeta ? newBetaVersion : newVersion;
  let npmTag = isBeta ? "@beta" : "";
  console.log("Found new webdav.client version " + newV);
  execSync("npm install webdav.client" + npmTag + " --save", {
    stdio: "inherit",
  });
  buildApp();
  copyClient();
}

function installAndBuild() {
  execSync("npm install", { stdio: "inherit" });
  buildApp();
  copyClient();
}

function buildApp() {
  try {
    if (isNetFramework) {
      execSync("npm run build:netframework", {
        stdio: "inherit",
      });
    } else {
      execSync("npm run build", {
        stdio: "inherit",
      });
    }
  } catch (err) {
    console.error(err);
  }
}

function copyClient() {
  if (!fs.existsSync("../wwwroot/webdav.client")) {
    fs.mkdirSync("../wwwroot/webdav.client");
  }

  try {
    execSync(
      `(robocopy node_modules/webdav.client ../wwwroot/webdav.client /E) ^& IF %ERRORLEVEL% LEQ 1 exit 0`,
      { stdio: "inherit" }
    );
    console.log("The documentation was copied successfully!");
  } catch (err) {
    console.error(err);
  }
}

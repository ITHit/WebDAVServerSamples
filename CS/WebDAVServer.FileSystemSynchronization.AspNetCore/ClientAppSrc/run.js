const { argv } = require("process");
const fs = require("fs");
const { execSync } = require("child_process");

let isNetFramework = argv[2] == "true" ? true : false;

let currVersion;
let newVersion;
if (fs.existsSync(".\\node_modules\\webdav.client")) {
  currVersion = require("webdav.client/package.json").version;
  newVersion = execSync("npm show webdav.client version").toString().trim();
}

if (newVersion == currVersion) {
  console.log("webdav.client version is up to date");
  if (currVersion == undefined) {
    execSync("npm install webdav.client --save", { stdio: "inherit" });
    installAndBuild();
  }

  if (!fs.existsSync("..\\wwwroot\\app.js")) {
    installAndBuild();
  }
} else {
  console.log("Found new webdav.client version " + newVersion);
  execSync("npm install webdav.client --save", { stdio: "inherit" });
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
      execSync("npm run build:netframework & npm run postbuild", {
        stdio: "inherit",
      });
    } else {
      execSync("npm run build & npm run postbuild", {
        stdio: "inherit",
      });
    }
  } catch (err) {
    console.error(err);
  }
}

function copyClient() {
  if (!fs.existsSync("..\\wwwroot\\webdav.client")) {
    fs.mkdirSync("..\\wwwroot\\webdav.client");
  }

  try {
    execSync(
      "(robocopy node_modules\\webdav.client ..\\wwwroot\\webdav.client /E) ^& IF %ERRORLEVEL% LEQ 1 exit 0",
      { stdio: "inherit" }
    );
    console.log("The documentation was copied successfully!");
  } catch (err) {
    console.error(err);
  }
}

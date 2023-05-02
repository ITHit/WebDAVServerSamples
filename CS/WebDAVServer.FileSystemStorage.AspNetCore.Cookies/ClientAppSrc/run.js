const { argv } = require("process");
const fs = require("fs");
const { execSync } = require("child_process");

let isNetFramework = argv[2] == "true" ? true : false;
let isGsuite = argv[3] == "true" ? true : false;

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
    let command = "npm run build";

    if (isNetFramework) command += ":netframework";

    if (isGsuite) command += ":gsuite";

    execSync(command, {
      stdio: "inherit",
    });
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

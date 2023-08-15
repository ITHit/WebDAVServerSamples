const process = require("process");
// npm install rewire
const rewire = require("rewire");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const defaults = rewire("react-scripts/scripts/build.js");
const config = defaults.__get__("config");

const argv = (key) => {
  // Return true if the key exists and a value is defined
  if (process.argv.includes(`--${key}`)) return true;

  const value = process.argv.find((element) => element.startsWith(`--${key}=`));

  // Return null if the key does not exist and a value is not defined
  if (!value) return null;

  return value.replace(`--${key}=`, "");
};

// Consolidate chunk files instead
config.optimization.splitChunks = {
  cacheGroups: {
    default: false,
  },
};
// Move runtime into bundle instead of separate file
config.optimization.runtimeChunk = false;

// JS
config.output.filename = "app.js";
config.output.chunkFilename = "app.[contenthash:8].chunk.js";

//Resources
config.output.assetModuleFilename = "images/[name][ext]";
config.output.publicPath = argv("netframework") ? "/wwwroot/" : "/";

config.module.rules[1].oneOf[2].use[1].options.name = "images/[name].[ext]";

//CSS
var miniCssPlugin = config.plugins.filter(
  (plugin) => plugin instanceof MiniCssExtractPlugin
)[0];

miniCssPlugin.options.filename = "app.css";
miniCssPlugin.options.chunkFilename = "app.[contenthash:8].chunk.css";

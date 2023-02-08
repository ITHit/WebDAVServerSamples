const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const CopyPlugin = require("copy-webpack-plugin");
const TerserPlugin = require("terser-webpack-plugin");
const path = require("path");
const webpack = require("webpack");
const fs = require("fs");

const dev = process.env.NODE_ENV !== "production";
const outputFolder = "../wwwroot/";

module.exports = (env) => {
  return {
    name: "default",
    //devtool: dev ? "eval-cheap-module-source-map" : "source-map",
    target: "web",
    entry: {
      app: ["./src/index.js"],
    },
    output: {
      path: path.resolve(__dirname, outputFolder),
      filename: "app.js",
    },
    cache: {
      type: "filesystem",
    },
    optimization: {
      minimize: true,
      minimizer: [new TerserPlugin(), new CssMinimizerPlugin()],
    },
    module: {
      rules: [
        {
          test: /\.s[ac]ss$/i,
          use: [
            MiniCssExtractPlugin.loader,
            {
              loader: "css-loader",
              options: {
                url: false,
              },
            },
            {
              loader: "sass-loader",
              options: {
                additionalData: env.IMAGES_PATH
                  ? "$image-base-path: '" +
                    JSON.stringify(env.IMAGES_PATH) +
                    "';"
                  : "",
              },
            },
          ],
        },
        {
          test: /\.(png|svg|jpg|jpeg|gif)$/i,
          type: "asset/resource",
        },
        {
          test: /\.(html)$/,
          include: path.join(__dirname, "html"),
          use: {
            loader: "html-loader",
            options: {
              minimize: false,
              interpolate: false,
            },
          },
        },
      ],
    },

    plugins: [
      new MiniCssExtractPlugin(),
      new HtmlWebpackPlugin({
        inject: false,
        template: "./app.html",
        filename: "app.html",
        minify: false,
        templateParameters: {
          imagesPath: env.IMAGES_PATH ? env.IMAGES_PATH : "/images/",
          leftPanel: fs.readFileSync("./src/html/leftPanel.html"),
          rightPanel: env.ISGSUITE
            ? fs.readFileSync("./src/html/rightPanel.html")
            : "",
        },
      }),
      new CopyPlugin({
        patterns: [{ from: "index.html" }],
      }),
      new CopyPlugin({
        patterns: [
          { from: "src/images", to: "images" },
          { from: "src/favicon.ico", to: "favicon.ico" },
        ],
      }),
      new webpack.DefinePlugin({
        "process.env": {
          ISGSUITE: env.ISGSUITE ? true : false,
          IMAGES_PATH: JSON.stringify(env.IMAGES_PATH),
        },
      }),
      new webpack.ContextReplacementPlugin(/moment[\/\\]locale$/, /en/),
    ],
  };
};

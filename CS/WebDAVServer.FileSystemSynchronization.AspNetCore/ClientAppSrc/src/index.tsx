import React from "react";
import ReactDOM from "react-dom";
import "./i18n/config";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import { Provider } from "react-redux";
import { store } from "./app/store";
import { getPath } from "./app/routerPaths";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import "./styles/scss/skin_base/app.scss";
import { WebSocketConnect } from "./services/WebSocketService";
//import { ITHit } from "webdav.client"

//if (window.webDavSettings && window.webDavSettings.LicenseId && !ITHit.WebDAV.Client.LicenseId) {
//  ITHit.WebDAV.Client.LicenseId = window.webDavSettings.LicenseId;
//}

WebSocketConnect();
ReactDOM.render(
  <Provider store={store}>
    <Router>
      <Routes>
        <Route path={getPath("home")} element={<App />} />
      </Routes>
    </Router>
  </Provider>,
  document.getElementById("app")
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

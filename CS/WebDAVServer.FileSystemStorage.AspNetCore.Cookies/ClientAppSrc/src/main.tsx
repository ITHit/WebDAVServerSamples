import "./webDavInitializeLicense.ts";
import { createRoot } from "react-dom/client";
import "./i18n/config.ts";
import App from "./App.tsx";
import { Provider } from "react-redux";
import { store } from "./app/store.ts";
import { getPath } from "./app/routerPaths.ts";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { WebSocketConnect } from "./services/WebSocketService.ts";
import "./styles/scss/skin_base/app.scss";

WebSocketConnect();
createRoot(document.getElementById("app")!).render(
  <Provider store={store}>
    <BrowserRouter
      future={{
        v7_startTransition: true,
        v7_relativeSplatPath: true,
      }}
    >
      <Routes>
        <Route path={getPath("home")} element={<App />} />
      </Routes>
    </BrowserRouter>
  </Provider>
);

import { EditDocAuth } from "./EditDocAuth";

export interface WindowWebDavSettings {
    WebDavServerPath: string | null;
    WebSocketPath: string | null;
    EditDocAuth: EditDocAuth | null;
    ApplicationProtocolsPath: string | null;
    WebDavServerVersion: string | null;
}
import {BackendMessage} from "./backendMessage";

export interface Webview {
    addEventListener: (callback: (data: BackendMessage) => void) => void;
    postMessage: (json: any) => void;
}
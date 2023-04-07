import { BehaviorSubject, Observable, Subject } from "rxjs";
import { isDevMode } from "./environment";
import { Webview } from "../interfaces/webview/webview";
import { InputMessage } from "../interfaces/webview/inputMessage";
import { OutputMessage } from "../interfaces/webview/outputMessage";

class WebviewServiceImpl {
    private readonly _messageSubject = new Subject<OutputMessage>();
    private readonly _message: Observable<OutputMessage>;

    public get message(): Observable<OutputMessage> {
        return this._message;
    }

    private get webview(): Webview {
        return {
            addEventListener: (callback: (data: any) => void) => (window as any).chrome.webview.addEventListener('message', callback),
            postMessage: (data: InputMessage) => (window as any).chrome.webview.postMessage(data)
        }
    }

    constructor() {
        this._message = this._messageSubject.asObservable();

        this.message.subscribe(message => {
            if (isDevMode()) console.log('Backend -> Frontend: ', message.type ? message.type : message.id, message.data);
        });

        this.webview.addEventListener(this.handleMessage.bind(this))
    }

    public postMessage(message: InputMessage) {
        this.webview.postMessage(message);

        if (!isDevMode()) return;
        console.log("Frontend -> Backend: ", message.id, message.method, message.data);
    }

    private handleMessage(message: any | undefined | null) {
        if (!message || !message.data || (!message.data.type && !message.data.id)) return;

        this._messageSubject.next(message.data)
    }
}

export const WebviewService = new WebviewServiceImpl();
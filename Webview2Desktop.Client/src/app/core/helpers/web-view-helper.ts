import { isDevMode } from "@angular/core";
import { BehaviorSubject, Observable, ReplaySubject, Subject } from "rxjs";
import { WebView } from "../interfaces/web-view";
import { WebViewMessage } from "../interfaces/web-view-message";

const INITIALIZED_TYPE = "initialized";

export class WebViewHelper {
	private _isInitialized: boolean = false;

	private _messageSubject: Subject<WebViewMessage> = new Subject<WebViewMessage>();
	public message: Observable<WebViewMessage>;

	private get webview(): WebView {
		return (window as any).chrome.webview
	}

	public get isReady(): boolean {
		const win = window as any;
		return win && win.chrome && win.chrome.webview && this._isInitialized;
	}

	constructor() {
		this.message = this._messageSubject.asObservable();
		this.message.subscribe(message => {
			if (isDevMode()) console.log("Backend message: ", message.type, message.data);

			switch (message.type) {
				case INITIALIZED_TYPE:
					this._isInitialized = true;
					console.log('WebView initialized');
					break;
			}
		})

		this.webview.addEventListener('message', this.handleMessage.bind(this));
	}

	public postMessage(message: WebViewMessage): boolean {
		if (this.isReady) return false;
		if (isDevMode()) console.log("Frontend message: ", message.type, message.data);

		this.webview.postMessage(message);
		return true;
	}

	private handleMessage(message: { data: WebViewMessage }) {
		if (!message) return;
		this._messageSubject.next(message.data);
	}
}

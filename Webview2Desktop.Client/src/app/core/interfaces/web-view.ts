export interface WebView {
	addEventListener: (type: string, callback: (event: any) => void) => void;
	postMessage: (json: any) => void;
}

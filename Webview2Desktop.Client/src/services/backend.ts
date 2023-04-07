import { filter, first, Observable, Subscription } from "rxjs";
import { WebviewService } from "./webview";
import { nanoid } from "nanoid";
import { OutputMessage } from "../interfaces/webview/outputMessage";

class BackendServiceImpl {
    private get messageChannel(): Observable<OutputMessage> {
        return WebviewService.message;
    }

    private _send(method: string, data: any | null | undefined): string {
        const id = nanoid();
        WebviewService.postMessage(
            !data ?
                { id, method } :
                { id, method, data }
        );
        return id;
    }

    private _receive(method: string, data: any, callback: (data: any) => void): Subscription {
        const subscription = this.messageChannel
            .pipe(
                filter((message: OutputMessage) => message.id === id),
                first()
            )
            .subscribe(callback);

        const id = this._send(method, data);
        return subscription;
    }

    private listen(type: string, customFilter: ((message: OutputMessage) => boolean) | undefined = undefined): Observable<OutputMessage> {
        return this.messageChannel
            .pipe(
                filter((message: OutputMessage) => message.type === type && (!customFilter ? true : customFilter(message)))
            );
    }

    public subscribe(type: string, callback: (response: OutputMessage) => void, filter: ((message: OutputMessage) => boolean) | undefined = undefined): Subscription {
        return this.listen(type, filter)
            .subscribe(message => callback(message))
    }

    public notify(type: string, data: any = undefined): string {
        return this._send(type, data);
    }

    public send(method: string, data: any = undefined): Promise<OutputMessage> {
        return new Promise((resolve) => {
            this._receive(
                method,
                data,
                resolve
            );
        })
    }
}

export const BackendService = new BackendServiceImpl();
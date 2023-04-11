import { filter, first, Observable, Subscription } from "rxjs";
import { WebviewService } from "./webview";
import { nanoid } from "nanoid";
import { OutputMessage } from "../interfaces/webview/outputMessage";
import { createQuery, Query } from "../interfaces/webview/bindings";

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

    private parseQuery(query: Query): [string | undefined, any | undefined] {
        const method = createQuery(query);
        if (!method) return [undefined, undefined];

        const data = (query as any)[method] as any;
        return [method, !data ? undefined : data];
    }

    public subscribe(query: Query, callback: (response: OutputMessage) => void, filter: ((message: OutputMessage) => boolean) | undefined = undefined): Subscription | OutputMessage {
        const [method, _] = this.parseQuery(query);
        if (!method) return {
            success: false,
            message: "Invalid query"
        };

        return this.listen(method, filter)
            .subscribe((message: OutputMessage) => callback(message))
    }

    public notify(query: Query): string | OutputMessage {
        const [method, data] = this.parseQuery(query);
        if (!method) return {
            success: false,
            message: "Invalid query"
        };

        return this._send(method, data);
    }

    public send(query: Query): Promise<OutputMessage> {
        return new Promise((resolve) => {
            const [method, data] = this.parseQuery(query);
            if (!method) return resolve({
                success: false,
                message: "Invalid query"
            });

            this._receive(
                method,
                !data ? undefined : data,
                resolve
            );
        })
    }
}

export const BackendService = new BackendServiceImpl();
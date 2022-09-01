import { Injectable } from '@angular/core';
import { filter, first, Observable, Subscription } from 'rxjs';
import { WebViewHelper } from '../../helpers/web-view-helper';
import { WebViewMessage } from '../../interfaces/web-view-message';

@Injectable({
  providedIn: 'root'
})
export class BackendService {
  private readonly webView: WebViewHelper;

  public get isReady(): boolean {
    return this.webView.isReady;
  }

  public get message(): Observable<WebViewMessage> {
    return this.webView.message;
  }

  constructor() {
    this.webView = new WebViewHelper();
  }

  private retrieve<T>(type: string, data: any | null = null): Promise<T> {
    return new Promise((resolve) => {
      this.webView.postMessage(
        !data ?
          {
            type
          } :
          {
            type,
            data
          }
      );

      this.message
        .pipe(filter(v => v.type === type), first())
        .subscribe(msg => {
          resolve(msg.data as T);
        });
    });
  }

  public sendMessage(type: string, data: any) {
    this.webView.postMessage({
      type,
      data
    });
  }

  public messageCSharp(message: string) {
    this.sendMessage("message-for-c#", message);
  }
}

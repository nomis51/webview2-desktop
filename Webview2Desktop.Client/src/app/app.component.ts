import { Component } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { filter } from 'rxjs';
import { BackendService } from './core/services/backend/backend.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  public receivedMessage: string = "";
  public messageField: FormControl = new FormControl('', [
    Validators.required
  ]);

  constructor(private backendService: BackendService) {
    this.backendService.message
      .pipe(filter(message => message.type === 'message-for-angular'))
      .subscribe(message => {
        this.receivedMessage = message.data as string;
      });
  }

  public talkToCSharp() {
    this.backendService.messageCSharp(this.messageField.value);
  }
}

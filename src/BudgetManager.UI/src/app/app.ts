import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { currentLanguage } from './i18n/i18n';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly language = currentLanguage;
}

import { Pipe, PipeTransform } from '@angular/core';
import { TranslationKey, TranslationParams, translate } from './i18n';

@Pipe({ name: 't' })
export class TranslatePipe implements PipeTransform {
  transform(key: TranslationKey, params?: TranslationParams): string {
    return translate(key, params);
  }
}

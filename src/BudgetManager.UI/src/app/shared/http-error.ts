import { HttpErrorResponse } from '@angular/common/http';
import { translate } from '../i18n/i18n';

export interface HttpErrorMessages {
  invalid: string;
  gone?: string;
  conflict?: string;
  fallback: string;
}

export function httpErrorMessage(response: HttpErrorResponse, messages: HttpErrorMessages): string {
  if (response.status === 422) {
    const body = response.error;
    const detail =
      typeof body === 'string' ? body : (body?.detail ?? body?.message ?? body?.title ?? '');
    return detail || messages.invalid;
  }
  if ((response.status === 403 || response.status === 404) && messages.gone) {
    return messages.gone;
  }
  if (response.status === 409 && messages.conflict) {
    return messages.conflict;
  }
  if (response.status === 429) {
    return translate('error.tooManyAttempts');
  }
  return messages.fallback;
}

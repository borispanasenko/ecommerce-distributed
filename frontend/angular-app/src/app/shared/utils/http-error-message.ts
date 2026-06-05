import { HttpErrorResponse } from '@angular/common/http';

export function getHttpErrorMessage(error: unknown, fallbackMessage: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallbackMessage;
  }

  const responseBody = error.error;

  if (typeof responseBody === 'string' && responseBody.trim()) {
    return responseBody;
  }

  if (typeof responseBody === 'object' && responseBody !== null) {
    const body = responseBody as {
      error?: string;
      message?: string;
      title?: string;
    };

    return body.message ?? body.error ?? body.title ?? fallbackMessage;
  }

  return fallbackMessage;
}
